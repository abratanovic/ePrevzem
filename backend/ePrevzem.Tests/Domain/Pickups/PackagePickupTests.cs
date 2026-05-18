using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackagePickupTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PickUpByCitizen_closes_placement_finalises_package_and_raises_event()
    {
        var pkg = PlacedPackage(out var recipient);
        var pickedUpAt = Now.AddDays(1);

        pkg.PickUpByCitizen(recipient, pickedUpAt);

        pkg.Status.Should().Be(PackageStatus.PickedUp);
        pkg.FinalizedAt.Should().Be(pickedUpAt);
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.PickedUpByCitizen);
        pkg.DomainEvents.OfType<PackagePickedUpByCitizen>().Should().ContainSingle();
    }

    [Fact]
    public void PickUpByCitizen_when_not_InLocker_throws()
    {
        var pkg = NewPackage(out _);  // AwaitingPlacement
        var act = () => pkg.PickUpByCitizen(CitizenUserId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    private static Package NewPackage(out CitizenUserId recipient)
    {
        recipient = CitizenUserId.New();
        return Package.Create(
            PackageId.New(), OrganizationId.New(), recipient, EmployeeAccountId.New(),
            PickupStationId.New(), "desc", Now);
    }

    private static Package PlacedPackage(out CitizenUserId recipient)
    {
        var pkg = NewPackage(out recipient);
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }
}
