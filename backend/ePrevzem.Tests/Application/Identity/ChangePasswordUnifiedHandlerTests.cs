using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.ChangeOrgAdminPassword;
using ePrevzem.Application.Identity.ChangePasswordUnified;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class ChangePasswordUnifiedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 10, 0, 0, TimeSpan.Zero);
    private const string CurrentPassword = "OldPass1!";
    private const string NewPassword = "NewPass2!";
    private const string CurrentHash = "hashed:OldPass1!";

    private static ChangePasswordUnifiedCommandHandler BuildHandler(
        ICurrentUser currentUser,
        TestOrgAdminRepoForUnifiedChange? orgAdminRepo = null,
        TestEmployeeRepoForUnifiedChange? employeeRepo = null)
        => new(
            orgAdminRepo ?? new TestOrgAdminRepoForUnifiedChange(),
            employeeRepo ?? new TestEmployeeRepoForUnifiedChange(),
            new TestPasswordHasherForUnifiedChange(),
            new TestUnitOfWorkForUnifiedChange(),
            new TestClockForUnifiedChange(Now),
            currentUser);

    private static OrganizationAdminAccount MakeOrgAdmin()
        => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "a@x.com", CurrentHash, Now);

    private static EmployeeAccount MakeEmployee()
    {
        var acc = EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(),
            "Jure", "Novak", "j@x.com",
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
        acc.SetPassword(CurrentHash, Now);
        return acc;
    }

    [Fact]
    public async Task Handle_org_admin_changes_password_and_clears_flag()
    {
        var account = MakeOrgAdmin();
        var orgAdminRepo = new TestOrgAdminRepoForUnifiedChange();
        orgAdminRepo.Add(account);
        var handler = BuildHandler(
            new FakeCurrentUser(account.Id.Value, "OrganizationAdmin"),
            orgAdminRepo: orgAdminRepo);

        await handler.Handle(new ChangePasswordUnifiedCommand(CurrentPassword, NewPassword), CancellationToken.None);

        account.PasswordHash.Should().Be("hashed:NewPass2!");
        account.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_employee_changes_password()
    {
        var account = MakeEmployee();
        var empRepo = new TestEmployeeRepoForUnifiedChange();
        empRepo.Add(account);
        var handler = BuildHandler(
            new FakeCurrentUser(account.Id.Value, "Employee"),
            employeeRepo: empRepo);

        await handler.Handle(new ChangePasswordUnifiedCommand(CurrentPassword, NewPassword), CancellationToken.None);

        account.PasswordHash.Should().Be("hashed:NewPass2!");
        account.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_wrong_current_password_throws()
    {
        var account = MakeOrgAdmin();
        var orgAdminRepo = new TestOrgAdminRepoForUnifiedChange();
        orgAdminRepo.Add(account);
        var handler = BuildHandler(
            new FakeCurrentUser(account.Id.Value, "OrganizationAdmin"),
            orgAdminRepo: orgAdminRepo);

        var act = () => handler.Handle(
            new ChangePasswordUnifiedCommand("WrongPassword!", NewPassword), CancellationToken.None);
        await act.Should().ThrowAsync<WrongCurrentPasswordException>();
    }

    [Fact]
    public async Task Handle_unauthenticated_throws()
    {
        var handler = BuildHandler(new FakeCurrentUser(null, null));
        var act = () => handler.Handle(
            new ChangePasswordUnifiedCommand(CurrentPassword, NewPassword), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public sealed class FakeCurrentUser : ICurrentUser
{
    private readonly Guid? _userId;
    private readonly string? _role;

    public FakeCurrentUser(Guid? userId, string? role) { _userId = userId; _role = role; }
    public Guid? UserId => _userId;
    public Guid? OrganizationId => null;
    public bool IsAuthenticated => _userId is not null;
    public bool IsInRole(string role) => _role == role;
}

public sealed class TestOrgAdminRepoForUnifiedChange : IOrganizationAdminAccountRepository
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

public sealed class TestEmployeeRepoForUnifiedChange : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();
    public void Add(EmployeeAccount a) => _items.Add(a);
    public Task<EmployeeAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == email));
    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
    public Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken ct = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));
    public Task AddAsync(EmployeeAccount account, CancellationToken ct = default) { _items.Add(account); return Task.CompletedTask; }
    public Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EmployeeAccount>>(_items.Where(x => x.OrganizationId == organisationId).ToList());
}

public sealed class TestPasswordHasherForUnifiedChange : IPasswordHasher
{
    public string Hash(string p) => $"hashed:{p}";
    public PasswordVerification Verify(string hash, string plaintext)
        => hash == $"hashed:{plaintext}" ? PasswordVerification.Success : PasswordVerification.Failed;
}

public sealed class TestClockForUnifiedChange : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForUnifiedChange(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForUnifiedChange : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}
