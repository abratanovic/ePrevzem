using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackagePlacementTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);

    [Fact]
    public void Place_opens_placement_sets_status_InLocker_and_computes_deadline()
    {
        var pkg = NewPackage();
        var placementId = PlacementId.New();
        var lockerId = LockerId.New();
        var employee = EmployeeAccountId.New();

        var placement = pkg.Place(placementId, lockerId, employee, FiveDays, Now.AddMinutes(1));

        pkg.Status.Should().Be(PackageStatus.InLocker);
        pkg.DeadlineAt.Should().Be(Now.AddMinutes(1) + FiveDays);
        placement.LockerId.Should().Be(lockerId);
        placement.OpenedByEmployeeAccountId.Should().Be(employee);
        placement.OpenedAt.Should().Be(Now.AddMinutes(1));
        placement.IsOpen.Should().BeTrue();
        pkg.ActivePlacement.Should().BeSameAs(placement);
        pkg.DomainEvents.OfType<PackagePlaced>().Should().ContainSingle();
    }

    [Fact]
    public void Place_when_not_AwaitingPlacement_throws()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), FiveDays, Now);

        var act = () => pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), FiveDays, Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*AwaitingPlacement*");
    }

    [Fact]
    public void Place_with_non_positive_duration_throws()
    {
        var pkg = NewPackage();
        var act = () => pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.Zero, Now);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pickupDuration");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);
}
