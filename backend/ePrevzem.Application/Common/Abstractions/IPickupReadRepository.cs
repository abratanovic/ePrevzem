using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Application.Common.Abstractions;

public interface IPickupReadRepository
{
    Task<IReadOnlyList<CitizenPickupResponse>> GetForCitizenAsync(
        CitizenUserId citizenId,
        DateTimeOffset now,
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
