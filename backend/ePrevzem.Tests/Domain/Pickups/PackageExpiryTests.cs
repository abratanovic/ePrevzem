using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageExpiryTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkExpired_sets_NotPickedUp_when_deadline_passed_and_still_in_locker()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromHours(1));
        var observedAt = Now.AddHours(2);

        pkg.MarkExpired(observedAt);

        pkg.Status.Should().Be(PackageStatus.NotPickedUp);
        pkg.ActivePlacement.Should().NotBeNull();
        pkg.DomainEvents.OfType<PackageExpired>().Should().ContainSingle();
    }

    [Fact]
    public void MarkExpired_before_deadline_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.MarkExpired(Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*deadline*");
    }

    [Fact]
    public void MarkExpired_when_not_InLocker_throws()
    {
        var pkg = NewPackage();  // AwaitingPlacement
        var act = () => pkg.MarkExpired(Now.AddDays(99));
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    [Fact]
    public void RetrieveAfterExpiry_closes_placement_transitions_to_AwaitingPersonalPickup()
    {
        var pkg = ExpiredPackage();
        var employee = EmployeeAccountId.New();
        var retrievedAt = Now.AddDays(10);

        pkg.RetrieveAfterExpiry(employee, retrievedAt);

        pkg.Status.Should().Be(PackageStatus.AwaitingPersonalPickup);
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.RetrievedAfterExpiry);
        pkg.DomainEvents.OfType<PackageRetrievedAfterExpiry>().Should().ContainSingle();
    }

    [Fact]
    public void RetrieveAfterExpiry_when_not_NotPickedUp_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*NotPickedUp*");
    }

    [Fact]
    public void MarkPickedUpManually_after_personal_pickup_finalises_package()
    {
        var pkg = ExpiredPackage();
        pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(10));
        var employee = EmployeeAccountId.New();

        pkg.MarkPickedUpManually(employee, Now.AddDays(11));

        pkg.Status.Should().Be(PackageStatus.PickedUp);
        pkg.FinalizedAt.Should().Be(Now.AddDays(11));
        pkg.DomainEvents.OfType<PackageMarkedPickedUpManually>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPickedUpManually_when_not_AwaitingPersonalPickup_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.MarkPickedUpManually(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*AwaitingPersonalPickup*");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage(TimeSpan deadlineIn)
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), deadlineIn, Now);
        pkg.ClearDomainEvents();
        return pkg;
    }

    private static Package ExpiredPackage()
    {
        var pkg = PlacedPackage(TimeSpan.FromHours(1));
        pkg.MarkExpired(Now.AddHours(2));
        pkg.ClearDomainEvents();
        return pkg;
    }
}
