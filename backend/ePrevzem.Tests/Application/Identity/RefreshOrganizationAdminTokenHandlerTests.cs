using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Application.Identity.RefreshOrgAdminToken;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class RefreshOrganizationAdminTokenHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private const string PlaintextToken = "plain_token";
    private static readonly string TokenHash =
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(PlaintextToken)));

    private static RefreshOrganizationAdminTokenCommandHandler BuildHandler(
        TestRefreshTokenRepoForOrgRefresh? rtRepo = null,
        TestOrgAdminRepoForRefresh? orgAdminRepo = null,
        TestTokenServiceForOrgRefresh? tokenService = null)
    {
        if (rtRepo is null)
        {
            var account = MakeAccount();
            orgAdminRepo ??= new TestOrgAdminRepoForRefresh();
            orgAdminRepo.Add(account);
            rtRepo = new TestRefreshTokenRepoForOrgRefresh();
            rtRepo.Add(ActiveToken(account.Id));
        }
        else
        {
            orgAdminRepo ??= new TestOrgAdminRepoForRefresh();
        }

        return new RefreshOrganizationAdminTokenCommandHandler(
            rtRepo,
            orgAdminRepo,
            new TestUnitOfWorkForOrgRefresh(),
            new TestClockForOrgRefresh(Now),
            tokenService ?? new TestTokenServiceForOrgRefresh());
    }

    private static OrganizationAdminAccount MakeAccount()
        => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "ana@example.com", "hash", Now);

    private static RefreshToken ActiveToken(OrganizationAdminAccountId accountId)
        => RefreshToken.IssueForOrganizationAdmin(
            RefreshTokenId.New(), accountId, TokenHash, Now.AddDays(14), Now);

    [Fact]
    public async Task Handle_returns_new_tokens()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(new RefreshOrganizationAdminTokenCommand(PlaintextToken), CancellationToken.None);
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_with_unknown_token_throws()
    {
        var rtRepo = new TestRefreshTokenRepoForOrgRefresh();
        var handler = BuildHandler(rtRepo: rtRepo);
        var act = () => handler.Handle(new RefreshOrganizationAdminTokenCommand("unknown"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_system_admin_token_throws()
    {
        var rtRepo = new TestRefreshTokenRepoForOrgRefresh();
        var systemToken = RefreshToken.IssueForSystemAdmin(
            RefreshTokenId.New(), SystemAdminId.New(), TokenHash, Now.AddDays(14), Now);
        rtRepo.Add(systemToken);

        var handler = BuildHandler(rtRepo: rtRepo);
        var act = () => handler.Handle(new RefreshOrganizationAdminTokenCommand(PlaintextToken), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_already_revoked_token_throws()
    {
        var account = MakeAccount();
        var token = ActiveToken(account.Id);
        token.Revoke(Now.AddMinutes(1));

        var rtRepo = new TestRefreshTokenRepoForOrgRefresh();
        rtRepo.Add(token);
        var orgRepo = new TestOrgAdminRepoForRefresh();
        orgRepo.Add(account);

        var handler = BuildHandler(rtRepo: rtRepo, orgAdminRepo: orgRepo);
        var act = () => handler.Handle(new RefreshOrganizationAdminTokenCommand(PlaintextToken), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_expired_token_throws()
    {
        var account = MakeAccount();
        // issued 20 days ago, expired 1 day ago — already past expiry at Now
        var token = RefreshToken.IssueForOrganizationAdmin(
            RefreshTokenId.New(), account.Id, TokenHash, Now.AddDays(-1), Now.AddDays(-20));

        var rtRepo = new TestRefreshTokenRepoForOrgRefresh();
        rtRepo.Add(token);
        var orgRepo = new TestOrgAdminRepoForRefresh();
        orgRepo.Add(account);

        var handler = BuildHandler(rtRepo: rtRepo, orgAdminRepo: orgRepo);
        var act = () => handler.Handle(
            new RefreshOrganizationAdminTokenCommand(PlaintextToken),
            CancellationToken.None);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }
}

public sealed class TestRefreshTokenRepoForOrgRefresh : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _items = new();

    public void Add(RefreshToken token) => _items.Add(token);

    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.TokenHash == tokenHash));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _items.Add(refreshToken);
        return Task.CompletedTask;
    }
}

public sealed class TestOrgAdminRepoForRefresh : IOrganizationAdminAccountRepository
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

public sealed class TestTokenServiceForOrgRefresh : ITokenService
{
    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
        => new("admin_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
        => new("org_admin_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
        => new("emp_token", DateTimeOffset.UtcNow.AddMinutes(15));

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
        => new("new_plain", "new_hash", now.AddDays(14));
}

public sealed class TestClockForOrgRefresh : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForOrgRefresh(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForOrgRefresh : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
