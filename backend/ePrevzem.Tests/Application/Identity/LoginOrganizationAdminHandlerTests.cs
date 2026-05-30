using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.LoginOrgAdmin;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class LoginOrganizationAdminHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private const string Email = "ana@example.com";
    private const string Password = "Secret1!";
    private const string Hash = "hashed:Secret1!";

    private static LoginOrganizationAdminCommandHandler BuildHandler(
        TestOrgAdminRepoForLogin? repo = null,
        TestRefreshTokenRepoForOrgLogin? rtRepo = null,
        TestPasswordHasherForOrgLogin? hasher = null,
        TestTokenServiceForOrgLogin? tokenService = null,
        TestUnitOfWorkForOrgLogin? uow = null)
        => new(
            repo ?? DefaultRepo(),
            rtRepo ?? new TestRefreshTokenRepoForOrgLogin(),
            uow ?? new TestUnitOfWorkForOrgLogin(),
            new TestClockForOrgLogin(Now),
            hasher ?? new TestPasswordHasherForOrgLogin(),
            tokenService ?? new TestTokenServiceForOrgLogin());

    private static TestOrgAdminRepoForLogin DefaultRepo()
    {
        var repo = new TestOrgAdminRepoForLogin();
        repo.Add(MakeAccount());
        return repo;
    }

    private static OrganizationAdminAccount MakeAccount(bool disabled = false)
    {
        var acc = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(),
            OrganizationId.New(),
            "Ana", "Kovač", Email, Hash, Now);
        if (disabled) acc.Disable(Now);
        return acc;
    }

    [Fact]
    public async Task Handle_returns_tokens_and_must_change_password()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(new LoginOrganizationAdminCommand(Email, Password), CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_wrong_password_throws()
    {
        var handler = BuildHandler();
        var act = () => handler.Handle(new LoginOrganizationAdminCommand(Email, "wrong"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_with_unknown_email_throws()
    {
        var handler = BuildHandler();
        var act = () => handler.Handle(new LoginOrganizationAdminCommand("unknown@x.com", Password), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_with_disabled_account_throws()
    {
        var repo = new TestOrgAdminRepoForLogin();
        repo.Add(MakeAccount(disabled: true));
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(new LoginOrganizationAdminCommand(Email, Password), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_persists_refresh_token()
    {
        var rtRepo = new TestRefreshTokenRepoForOrgLogin();
        var handler = BuildHandler(rtRepo: rtRepo);

        await handler.Handle(new LoginOrganizationAdminCommand(Email, Password), CancellationToken.None);

        rtRepo.Items.Should().HaveCount(1);
        rtRepo.Items[0].OrganizationAdminAccountId.Should().NotBeNull();
        rtRepo.Items[0].SystemAdminId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_must_change_password_is_false_after_set_password()
    {
        var acc = MakeAccount();
        acc.SetPassword(Hash, Now);
        var repo = new TestOrgAdminRepoForLogin();
        repo.Add(acc);
        var handler = BuildHandler(repo: repo);

        var result = await handler.Handle(new LoginOrganizationAdminCommand(Email, Password), CancellationToken.None);

        result.MustChangePassword.Should().BeFalse();
    }
}

public sealed class TestOrgAdminRepoForLogin : IOrganizationAdminAccountRepository
{
    private readonly List<OrganizationAdminAccount> _items = new();

    public void Add(OrganizationAdminAccount account) => _items.Add(account);

    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.Any(x => x.Email == normalizedEmail));

    public Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }
}

public sealed class TestRefreshTokenRepoForOrgLogin : IRefreshTokenRepository
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

public sealed class TestPasswordHasherForOrgLogin : IPasswordHasher
{
    public string Hash(string plaintext) => $"hashed:{plaintext}";
    public PasswordVerification Verify(string hash, string plaintext)
        => hash == $"hashed:{plaintext}" ? PasswordVerification.Success : PasswordVerification.Failed;
}

public sealed class TestTokenServiceForOrgLogin : ITokenService
{
    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
        => new("admin_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
        => new("org_admin_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
        => new("emp_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
        => new("refresh_plain", "refresh_hash", now.AddDays(14));
}

public sealed class TestClockForOrgLogin : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForOrgLogin(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForOrgLogin : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
