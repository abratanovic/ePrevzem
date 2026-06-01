using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed record GetOrganizationPickupStationQuery(Guid OrganizationId, Guid ClaimId)
    : IRequest<OrganizationPickupStationResponse>;

public sealed class GetOrganizationPickupStationQueryHandler
    : IRequestHandler<GetOrganizationPickupStationQuery, OrganizationPickupStationResponse>
{
    private readonly IStationClaimRepository _claimRepository;
    private readonly IPickupStationRepository _stationRepository;

    public GetOrganizationPickupStationQueryHandler(
        IStationClaimRepository claimRepository,
        IPickupStationRepository stationRepository)
    {
        _claimRepository = claimRepository;
        _stationRepository = stationRepository;
    }

    public async Task<OrganizationPickupStationResponse> Handle(
        GetOrganizationPickupStationQuery query,
        CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetActiveClaimByIdForOrganizationAsync(
            new StationClaimId(query.ClaimId),
            new OrganizationId(query.OrganizationId),
            cancellationToken)
            ?? throw new OrganizationPickupStationNotFoundException(query.ClaimId);
        var station = await _stationRepository.GetByIdAsync(claim.PickupStationId, cancellationToken)
            ?? throw new InvalidOperationException($"Pickup station '{claim.PickupStationId.Value}' was not found.");

        return OrganizationPickupStationResponse.From(claim, station);
    }
}
