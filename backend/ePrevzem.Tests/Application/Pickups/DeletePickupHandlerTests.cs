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
        var handler = new DeletePickupCommandHandler(repository, new TestPickupUnitOfWork());

        await handler.Handle(
            new DeletePickupCommand(organizationId.Value, package.Id.Value),
            CancellationToken.None);

        repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_pickup_from_another_organization_throws()
    {
        var package = NewPackage(OrganizationId.New());
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var handler = new DeletePickupCommandHandler(repository, new TestPickupUnitOfWork());

        var act = () => handler.Handle(
            new DeletePickupCommand(OrganizationId.New().Value, package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupNotFoundException>();
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_pickup_in_locker_throws()
    {
        var organizationId = OrganizationId.New();
        var package = NewPackage(organizationId);
        package.Place(
            PlacementId.New(),
            LockerId.New(),
            EmployeeAccountId.New(),
            TimeSpan.FromDays(5),
            Now);
        var repository = new TestPackageRepository();
        await repository.AddAsync(package);
        var handler = new DeletePickupCommandHandler(repository, new TestPickupUnitOfWork());

        var act = () => handler.Handle(
            new DeletePickupCommand(organizationId.Value, package.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<PickupDeletionForbiddenException>();
        repository.Items.Should().ContainSingle();
    }

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
