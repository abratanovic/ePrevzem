using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Insert;

/// <summary>
/// Operator opens a chosen free locker to insert a pending package. Validates
/// the operator, the org's claim on the package's target station, the package
/// state, and that the locker is free — then opens it. No state change; the
/// placement is persisted on confirm.
/// </summary>
public sealed record OpenInsertionLockerCommand(
    Guid OrganizationId,
    Guid ActorEmployeeId,
    Guid PackageId,
    Guid LockerId) : IRequest<LockerTokenResponse>;

public sealed class OpenInsertionLockerCommandHandler
    : IRequestHandler<OpenInsertionLockerCommand, LockerTokenResponse>
{
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IStationClaimRepository _stationClaimRepository;
    private readonly IPickupReadRepository _readRepository;
    private readonly ILockerGateway _lockerGateway;

    public OpenInsertionLockerCommandHandler(
        IEmployeeAccountRepository employeeRepository,
        IPackageRepository packageRepository,
        IStationClaimRepository stationClaimRepository,
        IPickupReadRepository readRepository,
        ILockerGateway lockerGateway)
    {
        _employeeRepository = employeeRepository;
        _packageRepository = packageRepository;
        _stationClaimRepository = stationClaimRepository;
        _readRepository = readRepository;
        _lockerGateway = lockerGateway;
    }

    public async Task<LockerTokenResponse> Handle(
        OpenInsertionLockerCommand command,
        CancellationToken cancellationToken)
    {
        var organizationId = new OrganizationId(command.OrganizationId);
        var lockerId = new LockerId(command.LockerId);

        var package = await InsertionGuard.ResolvePlaceablePackageAsync(
            command.ActorEmployeeId, organizationId, new PackageId(command.PackageId),
            _employeeRepository, _packageRepository, _stationClaimRepository, cancellationToken);

        var boxId = await _readRepository.GetFreeLockerBoxIdAsync(
            package.TargetPickupStationId, lockerId, cancellationToken)
            ?? throw new LockerUnavailableException();

        var token = await _lockerGateway.OpenBoxAsync(boxId, cancellationToken);
        return new LockerTokenResponse(Convert.ToBase64String(token));
    }
}
