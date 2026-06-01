using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Create;
using ePrevzem.Application.Pickups.LookupRecipient;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Pickups;

public class CreatePickupHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private static readonly PickupStationId StationId = PickupStationId.New();
    private static readonly CitizenUser Citizen = CitizenUser.Onboard(
        CitizenUserId.New(), "Ana", "Kovač", "0101006500006", null, null, Now);

    [Fact]
    public async Task Handle_employee_with_record_manager_role_persists_pickup()
    {
        var employee = EmployeeWithRoles(EmployeeAccountRole.RecordManager);
        var packageRepo = new TestPackageRepository();
        var handler = BuildHandler(packageRepo: packageRepo, employee: employee);

        var result = await handler.Handle(ValidCommand(employee.Id.Value, "Employee"), CancellationToken.None);

        result.Reference.Should().Be("EP-2026-000123");
        packageRepo.Items.Should().ContainSingle();
        packageRepo.Items[0].CreatedByEmployeeAccountId.Should().Be(employee.Id);
        packageRepo.Items[0].CreatedByOrganizationAdminAccountId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_organization_admin_persists_pickup_with_admin_creator()
    {
        var admin = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrgId, "Skrbnik", "Organizacije",
            "admin@example.com", "hash", Now);
        var packageRepo = new TestPackageRepository();
        var handler = BuildHandler(packageRepo: packageRepo, admin: admin);

        await handler.Handle(ValidCommand(admin.Id.Value, "OrganizationAdmin"), CancellationToken.None);

        packageRepo.Items.Should().ContainSingle();
        packageRepo.Items[0].CreatedByOrganizationAdminAccountId.Should().Be(admin.Id);
        packageRepo.Items[0].CreatedByEmployeeAccountId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_unknown_recipient_throws()
    {
        var handler = BuildHandler(includeCitizen: false, employee: EmployeeWithRoles(EmployeeAccountRole.RecordManager));

        var act = () => handler.Handle(ValidCommand(Guid.NewGuid(), "Employee"), CancellationToken.None);

        await act.Should().ThrowAsync<PickupRecipientNotFoundException>();
    }

    [Fact]
    public async Task Handle_station_from_another_organization_throws()
    {
        var handler = BuildHandler(
            stationOrganizationId: OrganizationId.New(),
            employee: EmployeeWithRoles(EmployeeAccountRole.RecordManager));

        var act = () => handler.Handle(ValidCommand(Guid.NewGuid(), "Employee"), CancellationToken.None);

        await act.Should().ThrowAsync<PickupStationForbiddenException>();
    }

    [Fact]
    public async Task Handle_employee_without_record_manager_role_throws()
    {
        var employee = EmployeeWithRoles(EmployeeAccountRole.Operator);
        var handler = BuildHandler(employee: employee);

        var act = () => handler.Handle(ValidCommand(employee.Id.Value, "Employee"), CancellationToken.None);

        await act.Should().ThrowAsync<PickupCreationForbiddenException>();
    }

    [Fact]
    public async Task Handle_reference_collision_generates_again()
    {
        var employee = EmployeeWithRoles(EmployeeAccountRole.RecordManager);
        var packageRepo = new TestPackageRepository(existingReferences: ["EP-2026-000123"]);
        var generator = new TestPackageReferenceGenerator("EP-2026-000123", "EP-2026-000124");
        var handler = BuildHandler(packageRepo: packageRepo, generator: generator, employee: employee);

        var result = await handler.Handle(ValidCommand(employee.Id.Value, "Employee"), CancellationToken.None);

        result.Reference.Should().Be("EP-2026-000124");
    }

    private static CreatePickupCommandHandler BuildHandler(
        TestPackageRepository? packageRepo = null,
        TestPackageReferenceGenerator? generator = null,
        bool includeCitizen = true,
        EmployeeAccount? employee = null,
        OrganizationAdminAccount? admin = null,
        OrganizationId? stationOrganizationId = null)
        => new(
            new TestCitizenRepository(includeCitizen ? Citizen : null),
            new TestPickupStationClaimRepository(StationId, stationOrganizationId ?? OrgId),
            new TestEmployeeRepository(employee),
            new TestOrganizationAdminRepository(admin),
            packageRepo ?? new TestPackageRepository(),
            generator ?? new TestPackageReferenceGenerator("EP-2026-000123"),
            new TestPickupUnitOfWork(),
            new TestPickupClock());

    private static CreatePickupCommand ValidCommand(Guid actorId, string actorRole)
        => new(OrgId.Value, actorId, actorRole, Citizen.Emso, StationId.Value, "Osebna izkaznica");

    private static EmployeeAccount EmployeeWithRoles(params EmployeeAccountRole[] roles)
        => EmployeeAccount.Create(
            EmployeeAccountId.New(),
            OrgId,
            "Test",
            "Zaposleni",
            "employee@example.com",
            roles,
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(),
            Now);
}

public sealed class TestCitizenRepository(CitizenUser? citizen) : ICitizenUserRepository
{
    public Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default)
        => Task.FromResult(citizen?.Emso == emso ? citizen : null);
    public Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class TestPickupStationClaimRepository : IStationClaimRepository
{
    private readonly StationClaim _claim;
    public TestPickupStationClaimRepository(PickupStationId stationId, OrganizationId organizationId)
    {
        _claim = StationClaim.Claim(
            StationClaimId.New(),
            stationId,
            organizationId,
            Location.Create(46m, 14m, "Koroška cesta", "46", "2000", "Maribor"),
            DateTimeOffset.UtcNow);
    }

    public Task<StationClaim?> GetActiveClaimForStationAsync(PickupStationId stationId, CancellationToken cancellationToken = default)
        => Task.FromResult(_claim.PickupStationId == stationId ? _claim : null);
    public Task<StationClaim?> GetActiveClaimByIdForOrganizationAsync(StationClaimId id, OrganizationId organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<StationClaim?>(null);
    public Task<IReadOnlyList<StationClaim>> GetActiveClaimsForOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StationClaim>>([]);
    public Task AddAsync(StationClaim claim, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class TestEmployeeRepository(EmployeeAccount? employee) : IEmployeeAccountRepository
{
    public Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(employee);
    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(employee?.Id == id ? employee : null);
    public Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EmployeeAccount>>([]);
}

public sealed class TestOrganizationAdminRepository(OrganizationAdminAccount? admin) : IOrganizationAdminAccountRepository
{
    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(admin?.Id == id ? admin : null);
    public Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(admin);
    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class TestPackageRepository(IEnumerable<string>? existingReferences = null) : IPackageRepository
{
    private readonly HashSet<string> _references = new(existingReferences ?? []);
    public List<Package> Items { get; } = [];

    public Task<bool> ExistsByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        => Task.FromResult(_references.Contains(reference));
    public Task AddAsync(Package package, CancellationToken cancellationToken = default)
    {
        Items.Add(package);
        return Task.CompletedTask;
    }
}

public sealed class TestPackageReferenceGenerator(params string[] references) : IPackageReferenceGenerator
{
    private int _index;
    public string Generate(DateTimeOffset now) => references[_index++];
}

public sealed class TestPickupUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

public sealed class TestPickupClock : IClock
{
    public DateTimeOffset UtcNow => new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
}
