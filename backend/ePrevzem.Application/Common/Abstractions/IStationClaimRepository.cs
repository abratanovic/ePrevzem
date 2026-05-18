using ePrevzem.Domain.Lockers;

namespace ePrevzem.Application.Common.Abstractions;

public interface IStationClaimRepository
{
    Task<StationClaim?> GetActiveClaimForStationAsync(PickupStationId stationId, CancellationToken cancellationToken = default);
    Task AddAsync(StationClaim claim, CancellationToken cancellationToken = default);
}
