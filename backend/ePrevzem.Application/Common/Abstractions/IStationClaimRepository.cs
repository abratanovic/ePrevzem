using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Common.Abstractions;

public interface IStationClaimRepository
{
    Task<StationClaim?> GetActiveClaimForStationAsync(PickupStationId stationId, CancellationToken cancellationToken = default);
    Task<StationClaim?> GetActiveClaimByIdForOrganizationAsync(
        StationClaimId id,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StationClaim>> GetActiveClaimsForOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);
    Task AddAsync(StationClaim claim, CancellationToken cancellationToken = default);
}
