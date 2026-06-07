using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
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
        => await GetAsync(organizationId, limit, cancellationToken);

    public async Task<IReadOnlyList<PickupResponse>> GetAllAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => await GetAsync(organizationId, null, cancellationToken);

    private async Task<IReadOnlyList<PickupResponse>> GetAsync(
        OrganizationId organizationId,
        int? limit,
        CancellationToken cancellationToken)
    {
        var query =
            from package in _dbContext.Packages.AsNoTracking()
            join citizen in _dbContext.CitizenUsers.AsNoTracking()
                on package.RecipientCitizenUserId equals citizen.Id
            join claim in _dbContext.StationClaims.AsNoTracking()
                on package.TargetPickupStationId equals claim.PickupStationId
            join station in _dbContext.PickupStations.AsNoTracking()
                on package.TargetPickupStationId equals station.Id
            where package.OrganizationId == organizationId
                && claim.OrganizationId == organizationId
                && claim.ClaimedAt <= package.CreatedAt
                && (claim.ReleasedAt == null || claim.ReleasedAt >= package.CreatedAt)
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
            };

        var rows = await (limit is null ? query : query.Take(limit.Value))
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

    public async Task<IReadOnlyList<CitizenPickupResponse>> GetForCitizenAsync(
        CitizenUserId citizenId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await CitizenPickupQuery()
            .Where(x => x.RecipientCitizenUserId == citizenId
                && (x.Status == PackageStatus.AwaitingPlacement
                    || x.Status == PackageStatus.InLocker
                    || x.Status == PackageStatus.AwaitingPersonalPickup))
            .OrderBy(x => x.DeadlineAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new CitizenPickupResponse(
            x.Id.Value,
            x.Reference,
            x.Description,
            x.OrganizationName,
            x.City,
            FormatAddress(x.Address, x.HouseNumber, x.ZipCode, x.City),
            x.OpenLockerNumber,
            x.Status.ToString(),
            x.DeadlineAt,
            x.CreatedAt,
            IsExpiringSoon(x.Status, x.DeadlineAt, now))).ToList();
    }

    public async Task<CitizenPickupDetailResponse?> GetCitizenPickupDetailAsync(
        CitizenUserId citizenId,
        PackageId packageId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var x = await CitizenPickupQuery()
            .Where(x => x.Id == packageId && x.RecipientCitizenUserId == citizenId)
            .FirstOrDefaultAsync(cancellationToken);

        if (x is null)
            return null;

        return new CitizenPickupDetailResponse(
            x.Id.Value,
            x.Reference,
            x.Description,
            x.OrganizationName,
            x.City,
            FormatAddress(x.Address, x.HouseNumber, x.ZipCode, x.City),
            x.LatestLockerNumber,
            x.Status.ToString(),
            x.DeadlineAt,
            x.CreatedAt,
            x.PickedUpAt,
            IsExpiringSoon(x.Status, x.DeadlineAt, now));
    }

    /// <summary>
    /// Shared projection for the two citizen-facing reads. Resolves the
    /// organization name, the station claim active at creation time (for the
    /// address), the currently-open locker number, the most recent locker
    /// number, and the pick-up timestamp — all without filtering by status so
    /// the detail read can return terminal pickups too.
    /// </summary>
    private IQueryable<CitizenPickupRow> CitizenPickupQuery()
        => from package in _dbContext.Packages.AsNoTracking()
           join org in _dbContext.Organizations.AsNoTracking()
               on package.OrganizationId equals org.Id
           join station in _dbContext.PickupStations.AsNoTracking()
               on package.TargetPickupStationId equals station.Id
           join claim in _dbContext.StationClaims.AsNoTracking()
               on package.TargetPickupStationId equals claim.PickupStationId
           where claim.OrganizationId == package.OrganizationId
               && claim.ClaimedAt <= package.CreatedAt
               && (claim.ReleasedAt == null || claim.ReleasedAt >= package.CreatedAt)
           select new CitizenPickupRow
           {
               Id = package.Id,
               RecipientCitizenUserId = package.RecipientCitizenUserId,
               Reference = package.Reference,
               Description = package.Description,
               OrganizationName = org.Name,
               Address = claim.Location.Address,
               HouseNumber = claim.Location.HouseNumber,
               ZipCode = claim.Location.ZipCode,
               City = claim.Location.City,
               Status = package.Status,
               DeadlineAt = package.DeadlineAt,
               CreatedAt = package.CreatedAt,
               OpenLockerNumber =
                   (from placement in _dbContext.Placements.AsNoTracking()
                    join locker in _dbContext.Lockers.AsNoTracking()
                        on placement.LockerId equals locker.Id
                    where placement.PackageId == package.Id && placement.EndedAt == null
                    select (int?)locker.LockerNumber).FirstOrDefault(),
               LatestLockerNumber =
                   (from placement in _dbContext.Placements.AsNoTracking()
                    join locker in _dbContext.Lockers.AsNoTracking()
                        on placement.LockerId equals locker.Id
                    where placement.PackageId == package.Id
                    orderby placement.OpenedAt descending
                    select (int?)locker.LockerNumber).FirstOrDefault(),
               PickedUpAt =
                   (from placement in _dbContext.Placements.AsNoTracking()
                    where placement.PackageId == package.Id
                        && placement.EndReason == PlacementEndReason.PickedUpByCitizen
                    select placement.EndedAt).FirstOrDefault()
           };

    public async Task<long?> GetActivePickupBoxIdAsync(
        CitizenUserId citizenId,
        PackageId packageId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from placement in _dbContext.Placements.AsNoTracking()
            join package in _dbContext.Packages.AsNoTracking()
                on placement.PackageId equals package.Id
            join locker in _dbContext.Lockers.AsNoTracking()
                on placement.LockerId equals locker.Id
            where package.Id == packageId
                && package.RecipientCitizenUserId == citizenId
                && package.Status == PackageStatus.InLocker
                && placement.EndedAt == null
            select (long?)locker.BoxId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InsertionContextResponse?> GetInsertionContextAsync(
        OrganizationId organizationId,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var station = await _dbContext.PickupStations.AsNoTracking()
            .Where(s => s.SerialNumber == serialNumber)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (station is null)
            return null;

        var claim = await _dbContext.StationClaims.AsNoTracking()
            .Where(c => c.PickupStationId == station.Id
                && c.OrganizationId == organizationId
                && c.ReleasedAt == null)
            .Select(c => new { c.Location.Address, c.Location.HouseNumber, c.Location.ZipCode, c.Location.City })
            .FirstOrDefaultAsync(cancellationToken);
        if (claim is null)
            return null;

        var packageRows = await (
            from package in _dbContext.Packages.AsNoTracking()
            join citizen in _dbContext.CitizenUsers.AsNoTracking()
                on package.RecipientCitizenUserId equals citizen.Id
            where package.OrganizationId == organizationId
                && package.TargetPickupStationId == station.Id
                && package.Status == PackageStatus.AwaitingPlacement
            orderby package.CreatedAt
            select new
            {
                package.Id,
                package.Reference,
                package.Description,
                RecipientName = citizen.FirstName + " " + citizen.LastName
            }).ToListAsync(cancellationToken);

        var occupiedLockerIds = _dbContext.Placements.AsNoTracking()
            .Where(p => p.EndedAt == null)
            .Select(p => p.LockerId);
        var lockerRows = await _dbContext.Lockers.AsNoTracking()
            .Where(l => l.PickupStationId == station.Id
                && l.IsServiceable
                && !occupiedLockerIds.Contains(l.Id))
            .OrderBy(l => l.LockerNumber)
            .Select(l => new { l.Id, l.LockerNumber })
            .ToListAsync(cancellationToken);

        return new InsertionContextResponse(
            station.Id.Value,
            serialNumber,
            FormatAddress(claim.Address, claim.HouseNumber, claim.ZipCode, claim.City),
            packageRows.Select(p => new InsertionPackageResponse(
                p.Id.Value, p.Reference, p.Description, p.RecipientName)).ToList(),
            lockerRows.Select(l => new FreeLockerResponse(l.Id.Value, l.LockerNumber)).ToList());
    }

    public async Task<long?> GetFreeLockerBoxIdAsync(
        PickupStationId stationId,
        LockerId lockerId,
        CancellationToken cancellationToken = default)
    {
        var locker = await _dbContext.Lockers.AsNoTracking()
            .Where(l => l.Id == lockerId && l.PickupStationId == stationId && l.IsServiceable)
            .Select(l => new { l.BoxId })
            .FirstOrDefaultAsync(cancellationToken);
        if (locker is null)
            return null;

        var occupied = await _dbContext.Placements.AsNoTracking()
            .AnyAsync(p => p.LockerId == lockerId && p.EndedAt == null, cancellationToken);
        return occupied ? null : locker.BoxId;
    }

    private static bool IsExpiringSoon(PackageStatus status, DateTimeOffset? deadlineAt, DateTimeOffset now)
        => status == PackageStatus.InLocker
            && deadlineAt is not null
            && deadlineAt.Value > now
            && deadlineAt.Value - now <= TimeSpan.FromHours(24);

    private sealed class CitizenPickupRow
    {
        public required PackageId Id { get; init; }
        public required CitizenUserId RecipientCitizenUserId { get; init; }
        public required string Reference { get; init; }
        public required string Description { get; init; }
        public required string OrganizationName { get; init; }
        public required string Address { get; init; }
        public required string HouseNumber { get; init; }
        public required string ZipCode { get; init; }
        public required string City { get; init; }
        public required PackageStatus Status { get; init; }
        public DateTimeOffset? DeadlineAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public int? OpenLockerNumber { get; init; }
        public int? LatestLockerNumber { get; init; }
        public DateTimeOffset? PickedUpAt { get; init; }
    }

    private static string FormatLocation(string serialNumber, string address, string houseNumber, string zipCode, string city)
        => $"{serialNumber} · {FormatAddress(address, houseNumber, zipCode, city)}";

    private static string FormatAddress(string address, string houseNumber, string zipCode, string city)
        => $"{address} {houseNumber}, {zipCode} {city}";
}
