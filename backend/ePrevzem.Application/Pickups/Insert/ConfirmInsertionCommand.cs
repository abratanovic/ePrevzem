using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Insert;

/// <summary>
/// Persists an operator insertion after the locker was opened and the package
/// physically placed ("Sem zaprl predalček"). Re-validates the operator, claim,
/// package state, and that the locker is still free, then places the package
/// (→ InLocker, deadline = org default pickup duration).
/// </summary>
public sealed record ConfirmInsertionCommand(
    Guid OrganizationId,
    Guid ActorEmployeeId,
    Guid PackageId,
    Guid LockerId) : IRequest<InsertionConfirmedResponse>;

public sealed class ConfirmInsertionCommandHandler
    : IRequestHandler<ConfirmInsertionCommand, InsertionConfirmedResponse>
{
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IStationClaimRepository _stationClaimRepository;
    private readonly IPickupReadRepository _readRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmInsertionCommandHandler(
        IEmployeeAccountRepository employeeRepository,
        IPackageRepository packageRepository,
        IStationClaimRepository stationClaimRepository,
        IPickupReadRepository readRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _employeeRepository = employeeRepository;
        _packageRepository = packageRepository;
        _stationClaimRepository = stationClaimRepository;
        _readRepository = readRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<InsertionConfirmedResponse> Handle(
        ConfirmInsertionCommand command,
        CancellationToken cancellationToken)
    {
        var organizationId = new OrganizationId(command.OrganizationId);
        var lockerId = new LockerId(command.LockerId);

        var package = await InsertionGuard.ResolvePlaceablePackageAsync(
            command.ActorEmployeeId, organizationId, new PackageId(command.PackageId),
            _employeeRepository, _packageRepository, _stationClaimRepository, cancellationToken);

        // Re-check the locker is still free (no reservation between open and confirm).
        var boxId = await _readRepository.GetFreeLockerBoxIdAsync(
            package.TargetPickupStationId, lockerId, cancellationToken);
        if (boxId is null)
            throw new LockerUnavailableException();

        var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new InsertionForbiddenException();

        package.Place(
            PlacementId.New(),
            lockerId,
            new EmployeeAccountId(command.ActorEmployeeId),
            organization.DefaultPickupDuration,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new InsertionConfirmedResponse(
            package.Id.Value,
            package.Reference,
            package.Status.ToString(),
            package.DeadlineAt);
    }
}
