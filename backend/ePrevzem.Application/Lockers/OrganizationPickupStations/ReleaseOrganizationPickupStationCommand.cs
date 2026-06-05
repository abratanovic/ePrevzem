using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed record ReleaseOrganizationPickupStationCommand(Guid OrganizationId, Guid ClaimId) : IRequest;

public sealed class ReleaseOrganizationPickupStationCommandHandler
    : IRequestHandler<ReleaseOrganizationPickupStationCommand>
{
    private readonly IStationClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReleaseOrganizationPickupStationCommandHandler(
        IStationClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(
        ReleaseOrganizationPickupStationCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetActiveClaimByIdForOrganizationAsync(
            new StationClaimId(command.ClaimId),
            new OrganizationId(command.OrganizationId),
            cancellationToken)
            ?? throw new OrganizationPickupStationNotFoundException(command.ClaimId);

        claim.Release(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
