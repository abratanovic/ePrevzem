using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.LoginUnified;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class LoginUnifiedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 10, 0, 0, TimeSpan.Zero);
    private const string OrgAdminEmail = "admin@example.com";
    private const string EmployeeEmail = "employee@example.com";
    private const string Password = "Secret1!";
    private const string Hash = "hashed:Secret1!";

    private static LoginUnifiedCommandHandler BuildHandler(
        TestOrgAdminRepoForUnifiedLogin? orgAdminRepo = null,
        TestEmployeeRepoForUnifiedLogin? employeeRepo = null,
        TestOrganizationRepoForUnifiedLogin? orgRepo = null,
        TestRefreshTokenRepoForUnifiedLogin? rtRepo = null,
        TestPasswordHasherForUnifiedLogin? hasher = null,
        TestTokenServiceForUnifiedLogin? tokenService = null,
        TestUnitOfWorkForUnifiedLogin? uow = null)
        => new(
            orgAdminRepo ?? new TestOrgAdminRepoForUnifiedLogin(),
            employeeRepo ?? new TestEmployeeRepoForUnifiedLogin(),
            orgRepo ?? new TestOrganizationRepoForUnifiedLogin(),
            rtRepo ?? new TestRefreshTokenRepoForUnifiedLogin(),
            uow ?? new TestUnitOfWorkForUnifiedLogin(),
            new TestClockForUnifiedLogin(Now),
            hasher ?? new TestPasswordHasherForUnifiedLogin(),
            tokenService ?? new TestTokenServiceForUnifiedLogin());

    private static OrganizationAdminAccount MakeOrgAdmin(bool disabled = false)
    {
        var acc = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", OrgAdminEmail, Hash, Now);
        if (disabled) acc.Disable(Now);
        return acc;
    }

    private static EmployeeAccount MakeEmployee(bool disabled = false)
    {
        var acc = EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(),
            "Jure", "Novak", EmployeeEmail,
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
        acc.SetPassword(Hash, Now);
        if (disabled) acc.Disable(Now);
        return acc;
    }

    [Fact]
    public async Task Handle_org_admin_happy_path_returns_OrganizationAdmin_role()
    {
        var repo = new TestOrgAdminRepoForUnifiedLogin();
        repo.Add(MakeOrgAdmin());
        var handler = BuildHandler(orgAdminRepo: repo);

        var result = await handler.Handle(new LoginUnifiedCommand(OrgAdminEmail, Password), CancellationToken.None);

        result.Role.Should().Be("OrganizationAdmin");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_employee_happy_path_returns_Employee_role()
    {
        var repo = new TestEmployeeRepoForUnifiedLogin();
        repo.Add(MakeEmployee());
        var handler = BuildHandler(employeeRepo: repo);

        var result = await handler.Handle(new LoginUnifiedCommand(EmployeeEmail, Password), CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_org_admin_must_change_password_propagated()
    {
        var acc = MakeOrgAdmin();
        var repo = new TestOrgAdminRepoForUnifiedLogin();
        repo.Add(acc);
        var handler = BuildHandler(orgAdminRepo: repo);

        var result = await handler.Handle(new LoginUnifiedCommand(OrgAdminEmail, Password), CancellationToken.None);

        result.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_employee_must_change_password_false_after_set_password()
    {
        var acc = MakeEmployee();
        var repo = new TestEmployeeRepoForUnifiedLogin();
        repo.Add(acc);
        var handler = BuildHandler(employeeRepo: repo);

        var result = await handler.Handle(new LoginUnifiedCommand(EmployeeEmail, Password), CancellationToken.None);

        result.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_neither_found_throws_InvalidCredentials()
    {
        var handler = BuildHandler();
        var act = () => handler.Handle(new LoginUnifiedCommand("unknown@x.com", Password), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_disabled_org_admin_throws_InvalidCredentials()
    {
        var repo = new TestOrgAdminRepoForUnifiedLogin();
        repo.Add(MakeOrgAdmin(disabled: true));
        var handler = BuildHandler(orgAdminRepo: repo);

        var act = () => handler.Handle(new LoginUnifiedCommand(OrgAdminEmail, Password), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_disabled_employee_throws_InvalidCredentials()
    {
        var repo = new TestEmployeeRepoForUnifiedLogin();
        repo.Add(MakeEmployee(disabled: true));
        var handler = BuildHandler(employeeRepo: repo);

        var act = () => handler.Handle(new LoginUnifiedCommand(EmployeeEmail, Password), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}

public sealed class TestOrgAdminRepoForUnifiedLogin : IOrganizationAdminAccountRepository
{
    private readonly List<OrganizationAdminAccount> _items = new();
    public void Add(OrganizationAdminAccount a) => _items.Add(a);
    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
    public Task<OrganizationAdminAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == email));
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_items.Any(x => x.Email == email));
    public Task AddAsync(OrganizationAdminAccount a, CancellationToken ct = default) { _items.Add(a); return Task.CompletedTask; }
}

public sealed class TestEmployeeRepoForUnifiedLogin : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();
    public void Add(EmployeeAccount a) => _items.Add(a);
    public Task<EmployeeAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == email));
    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
}

public sealed class TestOrganizationRepoForUnifiedLogin : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken ct = default)
        => Task.FromResult<Organization?>(null);
    public Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken ct = default)
        => Task.FromResult(false);
    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken ct = default)
        => Task.FromResult(false);
    public Task AddAsync(Organization organization, CancellationToken ct = default)
        => Task.CompletedTask;
}

public sealed class TestRefreshTokenRepoForUnifiedLogin : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = new();
    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken ct = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
    public Task<RefreshToken?> GetByTokenHashAsync(string hash, CancellationToken ct = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.TokenHash == hash));
    public Task AddAsync(RefreshToken t, CancellationToken ct = default) { Items.Add(t); return Task.CompletedTask; }
}

public sealed class TestPasswordHasherForUnifiedLogin : IPasswordHasher
{
    public string Hash(string p) => $"hashed:{p}";
    public PasswordVerification Verify(string hash, string plaintext)
        => hash == $"hashed:{plaintext}" ? PasswordVerification.Success : PasswordVerification.Failed;
}

public sealed class TestTokenServiceForUnifiedLogin : ITokenService
{
    public AccessTokenResult IssueAccessToken(SystemAdmin a) => new("sys_token", DateTimeOffset.UtcNow.AddMinutes(15));
    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount a) => new("org_token", DateTimeOffset.UtcNow.AddMinutes(15));
    public AccessTokenResult IssueAccessToken(EmployeeAccount e) => new("emp_token", DateTimeOffset.UtcNow.AddMinutes(15));
    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now) => new("refresh_plain", "refresh_hash", now.AddDays(14));
}

public sealed class TestClockForUnifiedLogin : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForUnifiedLogin(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForUnifiedLogin : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}
