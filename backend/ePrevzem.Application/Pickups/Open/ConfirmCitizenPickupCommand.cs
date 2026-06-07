using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Open;

/// <summary>
/// Commits a citizen pickup after the locker was opened and the user confirmed
/// collection ("Končaj"). Transitions the package to PickedUp and closes the
/// active placement. Throws if the package isn't theirs or isn't InLocker.
/// </summary>
public sealed record ConfirmCitizenPickupCommand(Guid CitizenUserId, Guid PickupId) : IRequest;

public sealed class ConfirmCitizenPickupCommandHandler : IRequestHandler<ConfirmCitizenPickupCommand>
{
    private readonly IPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmCitizenPickupCommandHandler(
        IPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(ConfirmCitizenPickupCommand command, CancellationToken cancellationToken)
    {
        var citizenId = new CitizenUserId(command.CitizenUserId);
        var package = await _packageRepository.GetByIdAsync(new PackageId(command.PickupId), cancellationToken);
        if (package is null || package.RecipientCitizenUserId != citizenId)
            throw new PickupNotFoundException(command.PickupId);

        package.PickUpByCitizen(citizenId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
