using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed record GetOrganizationPickupStationsQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<OrganizationPickupStationResponse>>;

public sealed class GetOrganizationPickupStationsQueryHandler
    : IRequestHandler<GetOrganizationPickupStationsQuery, IReadOnlyList<OrganizationPickupStationResponse>>
{
    private readonly IStationClaimRepository _claimRepository;
    private readonly IPickupStationRepository _stationRepository;

    public GetOrganizationPickupStationsQueryHandler(
        IStationClaimRepository claimRepository,
        IPickupStationRepository stationRepository)
    {
        _claimRepository = claimRepository;
        _stationRepository = stationRepository;
    }

    public async Task<IReadOnlyList<OrganizationPickupStationResponse>> Handle(
        GetOrganizationPickupStationsQuery query,
        CancellationToken cancellationToken)
    {
        var claims = await _claimRepository.GetActiveClaimsForOrganizationAsync(
            new OrganizationId(query.OrganizationId),
            cancellationToken);
        var responses = new List<OrganizationPickupStationResponse>(claims.Count);

        foreach (var claim in claims.OrderByDescending(claim => claim.ClaimedAt))
        {
            var station = await _stationRepository.GetByIdAsync(claim.PickupStationId, cancellationToken)
                ?? throw new InvalidOperationException($"Pickup station '{claim.PickupStationId.Value}' was not found.");
            responses.Add(OrganizationPickupStationResponse.From(claim, station));
        }

        return responses;
    }
}
