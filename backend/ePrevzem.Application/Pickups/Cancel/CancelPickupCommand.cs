using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Cancel;

public sealed record CancelPickupCommand(
    Guid OrganizationId,
    Guid ActorId,
    string ActorRole,
    Guid PickupId) : IRequest;

public sealed class CancelPickupCommandHandler : IRequestHandler<CancelPickupCommand>
{
    private readonly IPackageRepository _packageRepository;
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IOrganizationAdminAccountRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelPickupCommandHandler(
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

    public async Task Handle(CancelPickupCommand command, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.GetByIdForOrganizationAsync(
            new PackageId(command.PickupId),
            new OrganizationId(command.OrganizationId),
            cancellationToken)
            ?? throw new PickupNotFoundException(command.PickupId);

        if (command.ActorRole == "OrganizationAdmin")
        {
            var admin = await _adminRepository.GetByIdAsync(
                new OrganizationAdminAccountId(command.ActorId),
                cancellationToken);
            if (admin is null
                || admin.OrganizationId != package.OrganizationId
                || admin.Status != OrganizationAdminAccountStatus.Active)
                throw new PickupManagementForbiddenException();

            Cancel(package, admin.Id);
        }
        else if (command.ActorRole == "Employee")
        {
            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeAccountId(command.ActorId),
                cancellationToken);
            if (employee is null
                || employee.OrganizationId != package.OrganizationId
                || employee.Status != EmployeeAccountStatus.Active
                || !employee.CanManageRecords)
                throw new PickupManagementForbiddenException();

            Cancel(package, employee.Id);
        }
        else
        {
            throw new PickupManagementForbiddenException();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void Cancel(Package package, EmployeeAccountId actorId)
    {
        try
        {
            package.Cancel(actorId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new PickupCancellationForbiddenException(ex.Message);
        }
    }

    private void Cancel(Package package, OrganizationAdminAccountId actorId)
    {
        try
        {
            package.Cancel(actorId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new PickupCancellationForbiddenException(ex.Message);
        }
    }
}

public sealed class PickupCancellationForbiddenException(string message) : Exception(message);
