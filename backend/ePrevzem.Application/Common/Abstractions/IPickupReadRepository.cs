using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Application.Common.Abstractions;

public interface IPickupReadRepository
{
    Task<IReadOnlyList<CitizenPickupResponse>> GetForCitizenAsync(
        CitizenUserId citizenId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BoxId of the open placement's locker for a package the citizen owns and
    /// that is currently <c>InLocker</c>; null if not found / not theirs / not placed.
    /// </summary>
    Task<long?> GetActivePickupBoxIdAsync(
        CitizenUserId citizenId,
        PackageId packageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insertion context for an Operator scanning a station serial: resolves the
    /// station + the org's active claim, the packages awaiting placement targeted
    /// at this station, and the free (serviceable, unoccupied) lockers. Null when
    /// the station is unknown or the org has no active claim on it.
    /// </summary>
    Task<InsertionContextResponse?> GetInsertionContextAsync(
        OrganizationId organizationId,
        string serialNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BoxId of a locker that belongs to the station, is serviceable, and has no
    /// open placement; null otherwise. Used to open and to re-check on confirm.
    /// </summary>
    Task<long?> GetFreeLockerBoxIdAsync(
        PickupStationId stationId,
        LockerId lockerId,
        CancellationToken cancellationToken = default);

    Task<CitizenPickupDetailResponse?> GetCitizenPickupDetailAsync(
        CitizenUserId citizenId,
        PackageId packageId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PickupResponse>> GetRecentAsync(
        OrganizationId organizationId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PickupResponse>> GetAllAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PickupStationOptionResponse>> GetStationOptionsAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<DashboardStatsResponse> GetDashboardStatsAsync(
        OrganizationId organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LockerOccupancyResponse>> GetLockerOccupancyAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);
}
