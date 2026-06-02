using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Delete;

public sealed record DeletePickupCommand(Guid OrganizationId, Guid PickupId) : IRequest;

public sealed class DeletePickupCommandHandler : IRequestHandler<DeletePickupCommand>
{
    private readonly IPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePickupCommandHandler(
        IPackageRepository packageRepository,
        IUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePickupCommand command, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.GetByIdForOrganizationAsync(
            new PackageId(command.PickupId),
            new OrganizationId(command.OrganizationId),
            cancellationToken)
            ?? throw new PickupNotFoundException(command.PickupId);

        if (package.Status != PackageStatus.AwaitingPlacement)
            throw new PickupDeletionForbiddenException();

        _packageRepository.Remove(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PickupNotFoundException(Guid pickupId)
    : Exception($"Pickup '{pickupId}' was not found for this organization.");

public sealed class PickupDeletionForbiddenException()
    : Exception("Only pickups awaiting placement can be deleted.");
