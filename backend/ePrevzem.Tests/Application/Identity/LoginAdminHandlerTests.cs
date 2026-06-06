using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class LoginAdminHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_with_unknown_username_throws_invalid_credentials()
    {
        var systemAdminRepository = new TestSystemAdminRepository();
        var refreshTokenRepository = new TestRefreshTokenRepository();
        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            refreshTokenRepository,
            new TestUnitOfWork(),
            new TestClock(Now),
            new TestPasswordHasher(),
            new TestTokenService(),
            mediator);

        var command = new LoginAdminCommand("unknown-user", "password123");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        mediator.PublishedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SystemAdminLoginFailed>()
            .Which.AttemptedUsername.Should().Be("unknown-user");
    }

    [Fact]
    public async Task Handle_with_unknown_username_calls_verify_with_fake_hash()
    {
        var passwordHasher = new TestPasswordHasher();
        var systemAdminRepository = new TestSystemAdminRepository();
        var refreshTokenRepository = new TestRefreshTokenRepository();
        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            refreshTokenRepository,
            new TestUnitOfWork(),
            new TestClock(Now),
            passwordHasher,
            new TestTokenService(),
            mediator);

        var command = new LoginAdminCommand("unknown-user", "password123");

        try
        {
            await handler.Handle(command, CancellationToken.None);
        }
        catch (InvalidCredentialsException)
        {
            // Expected
        }

        passwordHasher.VerifyCallCount.Should().Be(1);
        passwordHasher.VerifyInvocations.Should().ContainSingle()
            .Which.Item2.Should().Be("password123");
    }

    [Fact]
    public async Task Handle_with_wrong_password_throws_invalid_credentials()
    {
        var passwordHasher = new TestPasswordHasher();
        var systemAdminRepository = new TestSystemAdminRepository();
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        systemAdminRepository.Add(admin);

        passwordHasher.SetVerifyResult("hash", "wrong", PasswordVerification.Failed);

        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            new TestRefreshTokenRepository(),
            new TestUnitOfWork(),
            new TestClock(Now),
            passwordHasher,
            new TestTokenService(),
            mediator);

        var act = () => handler.Handle(new LoginAdminCommand("ops-jane", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        mediator.PublishedEvents.Should().ContainSingle().Which.Should().BeOfType<SystemAdminLoginFailed>();
    }

    [Fact]
    public async Task Handle_with_correct_password_returns_token_response()
    {
        var passwordHasher = new TestPasswordHasher();
        var adminId = SystemAdminId.New();
        var hash = passwordHasher.Hash("correct");
        var systemAdminRepository = new TestSystemAdminRepository();
        var admin = SystemAdmin.Create(adminId, "ops-jane", hash, Now);
        systemAdminRepository.Add(admin);

        passwordHasher.SetVerifyResult(hash, "correct", PasswordVerification.Success);

        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            new TestRefreshTokenRepository(),
            new TestUnitOfWork(),
            new TestClock(Now),
            passwordHasher,
            new TestTokenService(),
            mediator);

        var result = await handler.Handle(new LoginAdminCommand("ops-jane", "correct"), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_with_correct_password_adds_refresh_token_to_db()
    {
        var passwordHasher = new TestPasswordHasher();
        var adminId = SystemAdminId.New();
        var hash = passwordHasher.Hash("correct");
        var systemAdminRepository = new TestSystemAdminRepository();
        var refreshTokenRepository = new TestRefreshTokenRepository();
        var admin = SystemAdmin.Create(adminId, "ops-jane", hash, Now);
        systemAdminRepository.Add(admin);

        passwordHasher.SetVerifyResult(hash, "correct", PasswordVerification.Success);

        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            refreshTokenRepository,
            new TestUnitOfWork(),
            new TestClock(Now),
            passwordHasher,
            new TestTokenService(),
            mediator);

        await handler.Handle(new LoginAdminCommand("ops-jane", "correct"), CancellationToken.None);

        refreshTokenRepository.Items.Should().HaveCount(1);
        refreshTokenRepository.Items[0].SystemAdminId.Should().Be(adminId);
    }

    [Fact]
    public async Task Handle_with_needs_rehash_updates_password_hash()
    {
        var passwordHasher = new TestPasswordHasher();
        var adminId = SystemAdminId.New();
        var oldHash = "old_hash";
        var systemAdminRepository = new TestSystemAdminRepository();
        var unitOfWork = new TestUnitOfWork();
        var admin = SystemAdmin.Create(adminId, "ops-jane", oldHash, Now);
        systemAdminRepository.Add(admin);

        passwordHasher.SetVerifyResult(oldHash, "correct", PasswordVerification.NeedsRehash);

        var mediator = new TestMediator();
        var handler = new LoginAdminCommandHandler(
            systemAdminRepository,
            new TestRefreshTokenRepository(),
            unitOfWork,
            new TestClock(Now),
            passwordHasher,
            new TestTokenService(),
            mediator);

        await handler.Handle(new LoginAdminCommand("ops-jane", "correct"), CancellationToken.None);

        admin.PasswordHash.Should().NotBe(oldHash);
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }
}

public sealed class TestClock : IClock
{
    private readonly DateTimeOffset _now;
    public TestClock(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestPasswordHasher : IPasswordHasher
{
    private Dictionary<string, PasswordVerification> _verifyResults = new();
    private int _hashCounter = 0;

    public int VerifyCallCount { get; private set; }
    public List<(string, string)> VerifyInvocations { get; } = new();

    public string Hash(string plaintext)
    {
        _hashCounter++;
        return $"hashed_{_hashCounter}_{plaintext}";
    }

    public PasswordVerification Verify(string hash, string plaintext)
    {
        VerifyCallCount++;
        VerifyInvocations.Add((hash, plaintext));

        var key = $"{hash}:{plaintext}";
        if (_verifyResults.TryGetValue(key, out var result))
            return result;

        if (hash.StartsWith("hashed_") && hash.Contains(plaintext))
            return PasswordVerification.Success;

        return PasswordVerification.Failed;
    }

    public void SetVerifyResult(string hash, string plaintext, PasswordVerification result)
        => _verifyResults[$"{hash}:{plaintext}"] = result;
}

public sealed class TestTokenService : ITokenService
{
    private int _accessTokenCounter = 0;
    private int _refreshTokenCounter = 0;

    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
    {
        _accessTokenCounter++;
        return new AccessTokenResult(
            $"access_token_{_accessTokenCounter}",
            new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));
    }

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
    {
        _accessTokenCounter++;
        return new AccessTokenResult(
            $"access_token_{_accessTokenCounter}",
            new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));
    }

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
        => new("emp_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
    {
        _refreshTokenCounter++;
        return new RefreshTokenResult(
            $"refresh_token_{_refreshTokenCounter}",
            $"refresh_token_hash_{_refreshTokenCounter}",
            now.AddDays(14));
    }
}

public sealed class TestMediator : IDomainEventDispatcher
{
    public List<object> PublishedEvents { get; } = new();

    public Task DispatchAsync(
        IReadOnlyCollection<ePrevzem.Domain.Common.IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        PublishedEvents.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}

public sealed class TestSystemAdminRepository : ISystemAdminRepository
{
    private readonly List<SystemAdmin> _items = new();

    public void Add(SystemAdmin admin) => _items.Add(admin);

    public Task<SystemAdmin?> GetByIdAsync(SystemAdminId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<SystemAdmin?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Username == normalizedUsername));

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_items.Count > 0);

    public Task AddAsync(SystemAdmin systemAdmin, CancellationToken cancellationToken = default)
    {
        _items.Add(systemAdmin);
        return Task.CompletedTask;
    }
}

public sealed class TestRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = new();

    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.TokenHash == tokenHash));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Items.Add(refreshToken);
        return Task.CompletedTask;
    }
}

public sealed class TestUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
