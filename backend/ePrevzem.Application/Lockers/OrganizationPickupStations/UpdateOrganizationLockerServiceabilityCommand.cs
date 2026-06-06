using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed record UpdateOrganizationLockerServiceabilityCommand(
    Guid OrganizationId,
    Guid ClaimId,
    Guid LockerId,
    bool IsServiceable) : IRequest<OrganizationPickupStationResponse>;

public sealed class UpdateOrganizationLockerServiceabilityCommandHandler
    : IRequestHandler<UpdateOrganizationLockerServiceabilityCommand, OrganizationPickupStationResponse>
{
    private readonly IStationClaimRepository _claimRepository;
    private readonly IPickupStationRepository _stationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateOrganizationLockerServiceabilityCommandHandler(
        IStationClaimRepository claimRepository,
        IPickupStationRepository stationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _claimRepository = claimRepository;
        _stationRepository = stationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<OrganizationPickupStationResponse> Handle(
        UpdateOrganizationLockerServiceabilityCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetActiveClaimByIdForOrganizationAsync(
            new StationClaimId(command.ClaimId),
            new OrganizationId(command.OrganizationId),
            cancellationToken)
            ?? throw new OrganizationPickupStationNotFoundException(command.ClaimId);
        var station = await _stationRepository.GetByIdAsync(claim.PickupStationId, cancellationToken)
            ?? throw new InvalidOperationException($"Pickup station '{claim.PickupStationId.Value}' was not found.");

        if (station.Lockers.All(x => x.Id.Value != command.LockerId))
            throw new OrganizationLockerNotFoundException(command.LockerId);

        station.SetLockerServiceability(new LockerId(command.LockerId), command.IsServiceable, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrganizationPickupStationResponse.From(claim, station);
    }
}
