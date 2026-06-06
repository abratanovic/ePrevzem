using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Delete;

public sealed record DeletePickupCommand(
    Guid OrganizationId,
    Guid ActorId,
    string ActorRole,
    Guid PickupId) : IRequest;

public sealed class DeletePickupCommandHandler : IRequestHandler<DeletePickupCommand>
{
    private readonly IPackageRepository _packageRepository;
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IOrganizationAdminAccountRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeletePickupCommandHandler(
        IPackageRepository packageRepository,
        IEmployeeAccountRepository employeeRepository,
        IOrganizationAdminAccountRepository adminRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _packageRepository = packageRepository;
        _employeeRepository = employeeRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(DeletePickupCommand command, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.GetByIdForOrganizationAsync(
            new PackageId(command.PickupId),
            new OrganizationId(command.OrganizationId),
            cancellationToken)
            ?? throw new PickupNotFoundException(command.PickupId);

        if (package.Status != PackageStatus.AwaitingPlacement || package.Placements.Count != 0)
            throw new PickupDeletionForbiddenException();

        await MarkDeleted(command, package, cancellationToken);

        _packageRepository.Remove(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkDeleted(
        DeletePickupCommand command,
        Package package,
        CancellationToken cancellationToken)
    {
        if (command.ActorRole == "OrganizationAdmin")
        {
            var admin = await _adminRepository.GetByIdAsync(
                new OrganizationAdminAccountId(command.ActorId),
                cancellationToken);
            if (admin is not null
                && admin.OrganizationId == package.OrganizationId
                && admin.Status == OrganizationAdminAccountStatus.Active)
            {
                package.MarkDeleted(admin.Id, _clock.UtcNow);
                return;
            }
        }
        else if (command.ActorRole == "Employee")
        {
            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeAccountId(command.ActorId),
                cancellationToken);
            if (employee is not null
                && employee.OrganizationId == package.OrganizationId
                && employee.Status == EmployeeAccountStatus.Active
                && employee.CanManageRecords)
            {
                package.MarkDeleted(employee.Id, _clock.UtcNow);
                return;
            }
        }

        throw new PickupManagementForbiddenException();
    }
}

public sealed class PickupNotFoundException(Guid pickupId)
    : Exception($"Pickup '{pickupId}' was not found for this organization.");

public sealed class PickupDeletionForbiddenException()
    : Exception("Only pickups awaiting placement with no placement history can be deleted.");

public sealed class PickupManagementForbiddenException()
    : Exception("The current user cannot manage pickups.");
