using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Pickups;

public class DeletePickupHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_pickup_awaiting_placement_removes_pickup()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = RecordManager(organizationId);
        var handler = BuildHandler(repository, employee);

        await handler.Handle(
            new DeletePickupCommand(organizationId.Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_pickup_from_another_organization_throws()
    {
        var package = NewPackage(OrganizationId.New());
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = RecordManager(package.OrganizationId);
        var handler = BuildHandler(repository, employee);

        var act = () => handler.Handle(
            new DeletePickupCommand(OrganizationId.New().Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupNotFoundException>();
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_pickup_with_placement_history_throws_even_after_removal()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        package.Place(
            PlacementId.New(),
            LockerId.New(),
            EmployeeAccountId.New(),
            TimeSpan.FromDays(5),
            Now);
        package.RemoveByEmployee(EmployeeAccountId.New(), Now.AddMinutes(1));
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = RecordManager(organizationId);
        var handler = BuildHandler(repository, employee);

        var act = () => handler.Handle(
            new DeletePickupCommand(organizationId.Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupDeletionForbiddenException>();
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_employee_without_record_manager_role_throws()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var employee = EmployeeAccount.Create(
            EmployeeAccountId.New(),
            organizationId,
            "Test",
            "Operator",
            "operator@example.com",
            [EmployeeAccountRole.Operator],
            [],
            ProvisioningCodeId.New(),
            Now);
        var handler = BuildHandler(repository, employee);

        var act = () => handler.Handle(
            new DeletePickupCommand(organizationId.Value, employee.Id.Value, "Employee", package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupManagementForbiddenException>();
        repository.Items.Should().ContainSingle();
    }

    private static DeletePickupCommandHandler BuildHandler(
        TestPackageRepository repository,
        EmployeeAccount? employee = null,
        OrganizationAdminAccount? admin = null)
        => new(
            repository,
            new TestEmployeeRepository(employee),
            new TestOrganizationAdminRepository(admin),
            new TestPickupUnitOfWork());

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
