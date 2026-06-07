using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Persistence;
using ePrevzem.Infrastructure.Pickups;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Tests.Infrastructure.Persistence;

public class PickupReadRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Dashboard_and_recent_pickups_are_derived_from_persisted_package_and_placement()
    {
        await using var db = CreateContext();
        var orgId = OrganizationId.New();
        var station = PickupStation.Create(PickupStationId.New(), "EP-PM-001", Now);
        var firstLocker = station.AddLocker(LockerId.New(), 1, 1001);
        station.AddLocker(LockerId.New(), 2, 1002);
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            station.Id,
            orgId,
            Location.Create(46m, 15m, "Koroška cesta", "46", "2000", "Maribor"),
            Now);
        var citizen = CitizenUser.Onboard(
            CitizenUserId.New(), "Ana", "Kovač", "0101006500006", null, null, Now);
        var package = Package.CreateByEmployee(
            PackageId.New(),
            orgId,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000123",
            "Osebna izkaznica",
            Now);
        package.Place(PlacementId.New(), firstLocker.Id, EmployeeAccountId.New(), TimeSpan.FromHours(2), Now);

        db.PickupStations.Add(station);
        db.StationClaims.Add(claim);
        db.CitizenUsers.Add(citizen);
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var repository = new PickupReadRepository(db);

        var stats = await repository.GetDashboardStatsAsync(orgId, Now);
        stats.ActivePickups.Should().Be(1);
        stats.PendingPickups.Should().Be(1);
        stats.PendingExpiresToday.Should().Be(1);
        stats.OccupiedLockers.Should().Be(1);
        stats.TotalLockers.Should().Be(2);

        var recent = await repository.GetRecentAsync(orgId, 10);
        recent.Should().ContainSingle();
        recent[0].Reference.Should().Be("EP-2026-000123");
        recent[0].RecipientName.Should().Be("Ana Kovač");
        recent[0].LocationName.Should().Contain("EP-PM-001");
        recent[0].CanDelete.Should().BeFalse();
        recent[0].CanCancel.Should().BeFalse();

        var occupancy = await repository.GetLockerOccupancyAsync(orgId);
        occupancy.Should().ContainSingle();
        occupancy[0].Used.Should().Be(1);
        occupancy[0].Total.Should().Be(2);
    }

    [Fact]
    public async Task All_pickups_returns_complete_history_without_recent_limit()
    {
        await using var db = CreateContext();
        var orgId = OrganizationId.New();
        var station = PickupStation.Create(PickupStationId.New(), "EP-PM-001", Now);
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            station.Id,
            orgId,
            Location.Create(46m, 15m, "Koroška cesta", "46", "2000", "Maribor"),
            Now);
        var citizen = CitizenUser.Onboard(
            CitizenUserId.New(), "Ana", "Kovač", "0101006500006", null, null, Now);
        var firstPackage = Package.CreateByEmployee(
            PackageId.New(),
            orgId,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000123",
            "Osebna izkaznica",
            Now);
        var secondPackage = Package.CreateByEmployee(
            PackageId.New(),
            orgId,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000124",
            "Potrdilo",
            Now.AddMinutes(1));

        db.PickupStations.Add(station);
        db.StationClaims.Add(claim);
        db.CitizenUsers.Add(citizen);
        db.Packages.AddRange(firstPackage, secondPackage);
        await db.SaveChangesAsync();

        var repository = new PickupReadRepository(db);

        var recent = await repository.GetRecentAsync(orgId, 1);
        var all = await repository.GetAllAsync(orgId);

        recent.Should().ContainSingle();
        recent[0].Reference.Should().Be("EP-2026-000124");
        all.Select(x => x.Reference).Should().Equal("EP-2026-000124", "EP-2026-000123");
    }

    [Fact]
    public async Task All_pickups_includes_pickup_created_before_station_was_released()
    {
        await using var db = CreateContext();
        var orgId = OrganizationId.New();
        var station = PickupStation.Create(PickupStationId.New(), "EP-PM-001", Now);
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            station.Id,
            orgId,
            Location.Create(46m, 15m, "Koroška cesta", "46", "2000", "Maribor"),
            Now);
        var citizen = CitizenUser.Onboard(
            CitizenUserId.New(), "Ana", "Kovač", "0101006500006", null, null, Now);
        var package = Package.CreateByEmployee(
            PackageId.New(),
            orgId,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000123",
            "Osebna izkaznica",
            Now);
        claim.Release(Now.AddDays(1));

        db.PickupStations.Add(station);
        db.StationClaims.Add(claim);
        db.CitizenUsers.Add(citizen);
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var repository = new PickupReadRepository(db);

        var all = await repository.GetAllAsync(orgId);

        all.Should().ContainSingle();
        all[0].Reference.Should().Be("EP-2026-000123");
    }

    private static EPrevzemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EPrevzemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPrevzemDbContext(options);
    }
}
