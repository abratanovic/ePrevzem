using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageCancellationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cancel_from_AwaitingPlacement_finalises_package()
    {
        var pkg = NewPackage();
        var employee = EmployeeAccountId.New();

        pkg.Cancel(employee, Now.AddMinutes(1));

        pkg.Status.Should().Be(PackageStatus.Cancelled);
        pkg.FinalizedAt.Should().Be(Now.AddMinutes(1));
        pkg.DomainEvents.OfType<PackageCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_from_AwaitingPersonalPickup_finalises_package()
    {
        var pkg = ExpiredAndRetrievedPackage();
        pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(15));
        pkg.Status.Should().Be(PackageStatus.Cancelled);
        pkg.FinalizedAt.Should().Be(Now.AddDays(15));
    }

    [Fact]
    public void Cancel_from_InLocker_throws_caller_must_remove_first()
    {
        var pkg = PlacedPackage();
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*remove*first*");
    }

    [Fact]
    public void Cancel_from_PickedUp_throws()
    {
        var pkg = PlacedPackage();
        pkg.PickUpByCitizen(CitizenUserId.New(), Now.AddDays(1));
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_twice_throws()
    {
        var pkg = NewPackage();
        pkg.Cancel(EmployeeAccountId.New(), Now.AddMinutes(1));
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddMinutes(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDeleted_on_fresh_package_raises_deleted_event()
    {
        var pkg = NewPackage();
        pkg.ClearDomainEvents();
        var employee = EmployeeAccountId.New();

        pkg.MarkDeleted(employee, Now.AddMinutes(1));

        var ev = pkg.DomainEvents.OfType<PackageDeleted>().Should().ContainSingle().Subject;
        ev.PackageId.Should().Be(pkg.Id);
        ev.OrganizationId.Should().Be(pkg.OrganizationId);
        ev.DeletedByEmployeeAccountId.Should().Be(employee);
        ev.DeletedByOrganizationAdminAccountId.Should().BeNull();
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromHours(1), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }

    private static Package ExpiredAndRetrievedPackage()
    {
        var pkg = PlacedPackage();
        pkg.MarkExpired(Now.AddHours(2));
        pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(10));
        pkg.ClearDomainEvents();
        return pkg;
    }
}
