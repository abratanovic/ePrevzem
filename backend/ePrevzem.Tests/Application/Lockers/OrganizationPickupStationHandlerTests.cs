using ePrevzem.Application.Lockers.OrganizationPickupStations;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Lockers;

public class OrganizationPickupStationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAll_returns_active_claims_for_requested_organization_with_station_lockers()
    {
        var organizationId = OrganizationId.New();
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var station = SeedStation(stationRepo, "EP-PM-001");
        SeedClaim(claimRepo, station.Id, organizationId);
        SeedClaim(claimRepo, PickupStationId.New(), OrganizationId.New());
        var handler = new GetOrganizationPickupStationsQueryHandler(claimRepo, stationRepo);

        var result = await handler.Handle(
            new GetOrganizationPickupStationsQuery(organizationId.Value),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].SerialNumber.Should().Be("EP-PM-001");
        result[0].Lockers.Should().ContainSingle()
            .Which.IsServiceable.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_for_different_organization_throws_not_found()
    {
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var station = SeedStation(stationRepo, "EP-PM-001");
        var claim = SeedClaim(claimRepo, station.Id, OrganizationId.New());
        var handler = new GetOrganizationPickupStationQueryHandler(claimRepo, stationRepo);

        var act = () => handler.Handle(
            new GetOrganizationPickupStationQuery(OrganizationId.New().Value, claim.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<OrganizationPickupStationNotFoundException>();
    }

    [Fact]
    public async Task UpdateLocation_replaces_location_and_saves()
    {
        var organizationId = OrganizationId.New();
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var unitOfWork = new TestUnitOfWorkForStation();
        var station = SeedStation(stationRepo, "EP-PM-001");
        var claim = SeedClaim(claimRepo, station.Id, organizationId);
        var handler = new UpdateOrganizationPickupStationLocationCommandHandler(
            claimRepo,
            stationRepo,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateOrganizationPickupStationLocationCommand(
                organizationId.Value,
                claim.Id.Value,
                46.0569m,
                14.5058m,
                "Dunajska cesta",
                "1",
                "1000",
                "Ljubljana"),
            CancellationToken.None);

        claim.Location.Address.Should().Be("Dunajska cesta");
        result.Location.Address.Should().Be("Dunajska cesta");
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Release_sets_released_at_and_saves()
    {
        var organizationId = OrganizationId.New();
        var claimRepo = new TestStationClaimRepository();
        var unitOfWork = new TestUnitOfWorkForStation();
        var claim = SeedClaim(claimRepo, PickupStationId.New(), organizationId);
        var handler = new ReleaseOrganizationPickupStationCommandHandler(
            claimRepo,
            unitOfWork,
            new TestClockForStation(Now));

        await handler.Handle(
            new ReleaseOrganizationPickupStationCommand(organizationId.Value, claim.Id.Value),
            CancellationToken.None);

        claim.ReleasedAt.Should().Be(Now);
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLockerServiceability_marks_locker_out_of_service_and_saves()
    {
        var organizationId = OrganizationId.New();
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var unitOfWork = new TestUnitOfWorkForStation();
        var station = SeedStation(stationRepo, "EP-PM-001");
        var claim = SeedClaim(claimRepo, station.Id, organizationId);
        var locker = station.Lockers.Single();
        var handler = new UpdateOrganizationLockerServiceabilityCommandHandler(
            claimRepo,
            stationRepo,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateOrganizationLockerServiceabilityCommand(
                organizationId.Value,
                claim.Id.Value,
                locker.Id.Value,
                false),
            CancellationToken.None);

        locker.IsServiceable.Should().BeFalse();
        result.Lockers.Should().ContainSingle().Which.IsServiceable.Should().BeFalse();
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    private static PickupStation SeedStation(TestPickupStationRepository repository, string serialNumber)
    {
        var station = PickupStation.Create(PickupStationId.New(), serialNumber, Now);
        station.AddLocker(LockerId.New(), 1);
        repository.Items.Add(station);
        return station;
    }

    private static StationClaim SeedClaim(
        TestStationClaimRepository repository,
        PickupStationId stationId,
        OrganizationId organizationId)
    {
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            stationId,
            organizationId,
            Location.Create(46m, 14m, "Slovenska cesta", "11", "1000", "Ljubljana"),
            Now);
        repository.Items.Add(claim);
        return claim;
    }
}
