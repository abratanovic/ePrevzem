using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.ClaimPickupStation;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Lockers;

public class ClaimPickupStationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgId = Guid.NewGuid();

    private static ClaimPickupStationCommandHandler BuildHandler(
        TestPickupStationRepository? stationRepo = null,
        TestStationClaimRepository? claimRepo = null,
        TestUnitOfWorkForStation? uow = null)
        => new(
            stationRepo ?? new TestPickupStationRepository(),
            claimRepo ?? new TestStationClaimRepository(),
            uow ?? new TestUnitOfWorkForStation(),
            new TestClockForStation(Now));

    private static ClaimPickupStationCommand ValidCommand(string serial = "SN-001")
        => new(OrgId, serial, 46.0569m, 14.5058m, "Slovenska cesta", "11", "1000", "Ljubljana");

    private static PickupStation SeedStation(
        TestPickupStationRepository repo,
        string serial = "SN-001")
    {
        var station = PickupStation.Create(PickupStationId.New(), serial, Now);
        station.AddLocker(LockerId.New(), 1);
        repo.Items.Add(station);
        return station;
    }

    [Fact]
    public async Task Handle_valid_command_persists_claim_with_correct_org_and_location()
    {
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        SeedStation(stationRepo);
        var handler = BuildHandler(stationRepo: stationRepo, claimRepo: claimRepo);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        claimRepo.Items.Should().HaveCount(1);
        claimRepo.Items[0].OrganizationId.Value.Should().Be(OrgId);
        claimRepo.Items[0].Location.Address.Should().Be("Slovenska cesta");
        claimRepo.Items[0].Location.City.Should().Be("Ljubljana");
        claimRepo.Items[0].Location.Latitude.Should().Be(46.0569m);
        claimRepo.Items[0].Location.Longitude.Should().Be(14.5058m);
    }

    [Fact]
    public async Task Handle_valid_command_returns_correct_response()
    {
        var stationRepo = new TestPickupStationRepository();
        SeedStation(stationRepo);
        var handler = BuildHandler(stationRepo: stationRepo);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.OrganizationId.Should().Be(OrgId);
        result.ClaimedAt.Should().Be(Now);
        result.Location.Address.Should().Be("Slovenska cesta");
        result.ClaimId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_valid_command_calls_save_changes()
    {
        var stationRepo = new TestPickupStationRepository();
        var uow = new TestUnitOfWorkForStation();
        SeedStation(stationRepo);
        var handler = BuildHandler(stationRepo: stationRepo, uow: uow);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_unknown_serial_number_throws_not_found()
    {
        var handler = BuildHandler();

        var act = () => handler.Handle(ValidCommand(serial: "UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<PickupStationNotFoundException>();
    }

    [Fact]
    public async Task Handle_already_claimed_station_throws_conflict()
    {
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var station = SeedStation(stationRepo);
        claimRepo.SeedActiveClaim(station.Id);
        var handler = BuildHandler(stationRepo: stationRepo, claimRepo: claimRepo);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<StationAlreadyClaimedException>();
    }

    [Fact]
    public async Task Handle_already_claimed_station_does_not_persist()
    {
        var stationRepo = new TestPickupStationRepository();
        var claimRepo = new TestStationClaimRepository();
        var station = SeedStation(stationRepo);
        claimRepo.SeedActiveClaim(station.Id);
        var handler = BuildHandler(stationRepo: stationRepo, claimRepo: claimRepo);

        try { await handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (StationAlreadyClaimedException) { }

        claimRepo.Items.Should().HaveCount(1);
    }
}

public sealed class TestStationClaimRepository : IStationClaimRepository
{
    public List<StationClaim> Items { get; } = new();
    private StationClaim? _activeClaim;

    public void SeedActiveClaim(PickupStationId stationId)
    {
        var location = Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");
        _activeClaim = StationClaim.Claim(
            StationClaimId.New(),
            stationId,
            OrganizationId.New(),
            location,
            DateTimeOffset.UtcNow);
        Items.Add(_activeClaim);
    }

    public Task<StationClaim?> GetActiveClaimForStationAsync(
        PickupStationId stationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_activeClaim?.PickupStationId == stationId ? _activeClaim : null);

    public Task<StationClaim?> GetActiveClaimByIdForOrganizationAsync(
        StationClaimId id,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x =>
            x.Id == id
            && x.OrganizationId == organizationId
            && x.ReleasedAt == null));

    public Task<IReadOnlyList<StationClaim>> GetActiveClaimsForOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StationClaim>>(Items
            .Where(x => x.OrganizationId == organizationId && x.ReleasedAt == null)
            .ToList());

    public Task AddAsync(StationClaim claim, CancellationToken cancellationToken = default)
    {
        Items.Add(claim);
        return Task.CompletedTask;
    }
}
