using ePrevzem.Application.Pickups.Cancel;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Pickups;

public class CancelPickupHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_record_manager_cancels_pickup_and_keeps_record()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = RecordManager(organizationId);
        var handler = BuildHandler(repository, employee: employee);

        await handler.Handle(
            new CancelPickupCommand(organizationId.Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        repository.Items.Should().ContainSingle();
        package.Status.Should().Be(PackageStatus.Cancelled);
        package.FinalizedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_organization_admin_cancels_pickup()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var admin = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(),
            organizationId,
            "Test",
            "Admin",
            "admin@example.com",
            "hash",
            Now);
        var handler = BuildHandler(repository, admin: admin);

        await handler.Handle(
            new CancelPickupCommand(organizationId.Value, admin.Id.Value, "OrganizationAdmin", package.Id.Value),
            CancellationToken.None);

        package.Status.Should().Be(PackageStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_pickup_in_locker_throws()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        package.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = RecordManager(organizationId);
        var handler = BuildHandler(repository, employee: employee);

        var act = () => handler.Handle(
            new CancelPickupCommand(organizationId.Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupCancellationForbiddenException>();
        package.Status.Should().Be(PackageStatus.InLocker);
    }

    private static CancelPickupCommandHandler BuildHandler(
        TestPackageRepository repository,
        EmployeeAccount? employee = null,
        OrganizationAdminAccount? admin = null)
        => new(
            repository,
            new TestEmployeeRepository(employee),
            new TestOrganizationAdminRepository(admin),
            new TestPickupUnitOfWork(),
            new TestPickupClock());

    private static EmployeeAccount RecordManager(OrganizationId organizationId)
        => EmployeeAccount.Create(
            EmployeeAccountId.New(),
            organizationId,
            "Test",
            "Manager",
            "manager@example.com",
            [EmployeeAccountRole.RecordManager],
            [],
            ProvisioningCodeId.New(),
            Now);

    private static Package NewPackage(OrganizationId organizationId)
        => Package.CreateByEmployee(
            PackageId.New(),
            organizationId,
            CitizenUserId.New(),
            EmployeeAccountId.New(),
            PickupStationId.New(),
            "EP-2026-000123",
            "Osebna izkaznica",
            Now);
}
