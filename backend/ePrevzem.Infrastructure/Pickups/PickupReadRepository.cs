using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Pickups;

public sealed class PickupReadRepository : IPickupReadRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public PickupReadRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PickupResponse>> GetRecentAsync(
        OrganizationId organizationId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from package in _dbContext.Packages.AsNoTracking()
            join citizen in _dbContext.CitizenUsers.AsNoTracking()
                on package.RecipientCitizenUserId equals citizen.Id
            join claim in _dbContext.StationClaims.AsNoTracking().Where(x => x.ReleasedAt == null)
                on package.TargetPickupStationId equals claim.PickupStationId
            join station in _dbContext.PickupStations.AsNoTracking()
                on package.TargetPickupStationId equals station.Id
            where package.OrganizationId == organizationId
                && claim.OrganizationId == organizationId
            orderby package.CreatedAt descending
            select new
            {
                package.Id,
                package.Reference,
                package.Description,
                RecipientName = citizen.FirstName + " " + citizen.LastName,
                station.SerialNumber,
                claim.Location.Address,
                claim.Location.HouseNumber,
                claim.Location.ZipCode,
                claim.Location.City,
                package.Status,
                package.DeadlineAt,
                package.CreatedAt,
                HasPlacementHistory = package.Placements.Any()
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new PickupResponse(
            x.Id.Value,
            x.Reference,
            x.Description,
            x.RecipientName,
            FormatLocation(x.SerialNumber, x.Address, x.HouseNumber, x.ZipCode, x.City),
            x.Status.ToString(),
            x.DeadlineAt,
            x.CreatedAt,
            x.Status == PackageStatus.AwaitingPlacement && !x.HasPlacementHistory,
            x.Status == PackageStatus.AwaitingPlacement
                || x.Status == PackageStatus.AwaitingPersonalPickup)).ToList();
    }

    public async Task<IReadOnlyList<PickupStationOptionResponse>> GetStationOptionsAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from claim in _dbContext.StationClaims.AsNoTracking()
            join station in _dbContext.PickupStations.AsNoTracking()
                on claim.PickupStationId equals station.Id
            where claim.OrganizationId == organizationId && claim.ReleasedAt == null
            orderby station.SerialNumber
            select new
            {
                station.Id,
                station.SerialNumber,
                claim.Location.Address,
                claim.Location.HouseNumber,
                claim.Location.ZipCode,
                claim.Location.City
            }).ToListAsync(cancellationToken);

        return rows.Select(x => new PickupStationOptionResponse(
            x.Id.Value,
            x.SerialNumber,
            FormatAddress(x.Address, x.HouseNumber, x.ZipCode, x.City))).ToList();
    }

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(
        OrganizationId organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var startOfToday = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var startOfTomorrow = startOfToday.AddDays(1);
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        var packages = _dbContext.Packages.AsNoTracking().Where(x => x.OrganizationId == organizationId);

        var activePickups = await packages.CountAsync(
            x => x.Status != PackageStatus.PickedUp && x.Status != PackageStatus.Cancelled,
            cancellationToken);
        var activePickupsTrend = await packages.CountAsync(x => x.CreatedAt >= thirtyDaysAgo, cancellationToken);
        var pendingPickups = await packages.CountAsync(x => x.Status == PackageStatus.InLocker, cancellationToken);
        var pendingExpiresToday = await packages.CountAsync(
            x => x.Status == PackageStatus.InLocker
                && x.DeadlineAt >= startOfToday
                && x.DeadlineAt < startOfTomorrow,
            cancellationToken);
        var expiredThisWeek = await packages.CountAsync(
            x => x.Status == PackageStatus.NotPickedUp && x.DeadlineAt >= sevenDaysAgo,
            cancellationToken);

        var activeStationIds = await _dbContext.StationClaims.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ReleasedAt == null)
            .Select(x => x.PickupStationId)
            .ToListAsync(cancellationToken);
        var lockerIds = await _dbContext.Lockers.AsNoTracking()
            .Where(x => activeStationIds.Contains(x.PickupStationId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var occupiedLockers = await _dbContext.Placements.AsNoTracking()
            .CountAsync(x => x.EndedAt == null && lockerIds.Contains(x.LockerId), cancellationToken);

        return new DashboardStatsResponse(
            activePickups,
            activePickupsTrend,
            pendingPickups,
            pendingExpiresToday,
            occupiedLockers,
            lockerIds.Count,
            expiredThisWeek);
    }

    public async Task<IReadOnlyList<LockerOccupancyResponse>> GetLockerOccupancyAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var claims = await _dbContext.StationClaims.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ReleasedAt == null)
            .ToListAsync(cancellationToken);
        var stationIds = claims.Select(x => x.PickupStationId).ToList();
        var stations = await _dbContext.PickupStations.AsNoTracking()
            .Where(x => stationIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var lockers = await _dbContext.Lockers.AsNoTracking()
            .Where(x => stationIds.Contains(x.PickupStationId))
            .ToListAsync(cancellationToken);
        var lockerIds = lockers.Select(x => x.Id).ToList();
        var occupiedLockerIds = await _dbContext.Placements.AsNoTracking()
            .Where(x => x.EndedAt == null && lockerIds.Contains(x.LockerId))
            .Select(x => x.LockerId)
            .ToListAsync(cancellationToken);

        return claims
            .Select(claim =>
            {
                var station = stations.Single(x => x.Id == claim.PickupStationId);
                var stationLockers = lockers.Where(x => x.PickupStationId == station.Id).ToList();
                return new LockerOccupancyResponse(
                    station.Id.Value,
                    station.SerialNumber,
                    FormatLocation(
                        station.SerialNumber,
                        claim.Location.Address,
                        claim.Location.HouseNumber,
                        claim.Location.ZipCode,
                        claim.Location.City),
                    stationLockers.Count(x => occupiedLockerIds.Contains(x.Id)),
                    stationLockers.Count);
            })
            .OrderBy(x => x.StationId)
            .ToList();
    }

    private static string FormatLocation(string serialNumber, string address, string houseNumber, string zipCode, string city)
        => $"{serialNumber} · {FormatAddress(address, houseNumber, zipCode, city)}";

    private static string FormatAddress(string address, string houseNumber, string zipCode, string city)
        => $"{address} {houseNumber}, {zipCode} {city}";
}
