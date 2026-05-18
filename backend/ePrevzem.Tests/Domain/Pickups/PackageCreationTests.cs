using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageCreationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_initialises_package_in_AwaitingPlacement_with_no_deadline()
    {
        var id = PackageId.New();
        var orgId = OrganizationId.New();
        var recipient = CitizenUserId.New();
        var createdBy = EmployeeAccountId.New();
        var station = PickupStationId.New();

        var pkg = Package.Create(id, orgId, recipient, createdBy, station, "Vabilo na sodišče", Now);

        pkg.Id.Should().Be(id);
        pkg.OrganizationId.Should().Be(orgId);
        pkg.RecipientCitizenUserId.Should().Be(recipient);
        pkg.CreatedByEmployeeAccountId.Should().Be(createdBy);
        pkg.TargetPickupStationId.Should().Be(station);
        pkg.Description.Should().Be("Vabilo na sodišče");
        pkg.Status.Should().Be(PackageStatus.AwaitingPlacement);
        pkg.DeadlineAt.Should().BeNull();
        pkg.CreatedAt.Should().Be(Now);
        pkg.FinalizedAt.Should().BeNull();
        pkg.Placements.Should().BeEmpty();
        pkg.ActivePlacement.Should().BeNull();
        pkg.DomainEvents.OfType<PackageCreated>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_description_throws(string description)
    {
        var act = () => Package.Create(
            PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
            PickupStationId.New(), description, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }
}
