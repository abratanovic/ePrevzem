using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageRemovalTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RemoveByEmployee_closes_placement_clears_deadline_and_returns_to_AwaitingPlacement()
    {
        var pkg = PlacedPackage();
        var employee = EmployeeAccountId.New();
        var removedAt = Now.AddHours(1);

        pkg.RemoveByEmployee(employee, removedAt);

        pkg.Status.Should().Be(PackageStatus.AwaitingPlacement);
        pkg.DeadlineAt.Should().BeNull();
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.RemovedByEmployee);
        pkg.DomainEvents.OfType<PackageRemovedByEmployee>().Should().ContainSingle();
    }

    [Fact]
    public void RemoveByEmployee_then_Place_again_starts_new_placement_with_fresh_deadline()
    {
        var pkg = PlacedPackage();
        pkg.RemoveByEmployee(EmployeeAccountId.New(), Now.AddHours(1));
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now.AddHours(2));

        pkg.Status.Should().Be(PackageStatus.InLocker);
        pkg.Placements.Should().HaveCount(2);
        pkg.ActivePlacement.Should().NotBeNull();
        pkg.DeadlineAt.Should().Be(Now.AddHours(2) + TimeSpan.FromDays(5));
    }

    [Fact]
    public void RemoveByEmployee_when_not_InLocker_throws()
    {
        var pkg = NewPackage();  // AwaitingPlacement
        var act = () => pkg.RemoveByEmployee(EmployeeAccountId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }
}
