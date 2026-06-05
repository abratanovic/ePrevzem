using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Common.Abstractions;

public interface IPickupReadRepository
{
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
