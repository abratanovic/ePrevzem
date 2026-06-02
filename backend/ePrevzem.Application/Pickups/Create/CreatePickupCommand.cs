using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Application.Pickups.LookupRecipient;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Create;

public sealed record CreatePickupCommand(
    Guid OrganizationId,
    Guid ActorId,
    string ActorRole,
    string RecipientEmso,
    Guid TargetPickupStationId,
    string Description) : IRequest<PickupResponse>;

public sealed class CreatePickupCommandHandler : IRequestHandler<CreatePickupCommand, PickupResponse>
{
    private const int MaxReferenceAttempts = 10;

    private readonly ICitizenUserRepository _citizenRepository;
    private readonly IStationClaimRepository _stationClaimRepository;
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IOrganizationAdminAccountRepository _adminRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IPackageReferenceGenerator _referenceGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePickupCommandHandler(
        ICitizenUserRepository citizenRepository,
        IStationClaimRepository stationClaimRepository,
        IEmployeeAccountRepository employeeRepository,
        IOrganizationAdminAccountRepository adminRepository,
        IPackageRepository packageRepository,
        IPackageReferenceGenerator referenceGenerator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _citizenRepository = citizenRepository;
        _stationClaimRepository = stationClaimRepository;
        _employeeRepository = employeeRepository;
        _adminRepository = adminRepository;
        _packageRepository = packageRepository;
        _referenceGenerator = referenceGenerator;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PickupResponse> Handle(CreatePickupCommand command, CancellationToken cancellationToken)
    {
        var organizationId = new OrganizationId(command.OrganizationId);
        var stationId = new PickupStationId(command.TargetPickupStationId);
        var recipient = await _citizenRepository.GetByEmsoAsync(command.RecipientEmso.Trim(), cancellationToken)
            ?? throw new PickupRecipientNotFoundException();

        var claim = await _stationClaimRepository.GetActiveClaimForStationAsync(stationId, cancellationToken);
        if (claim is null || claim.OrganizationId != organizationId)
            throw new PickupStationForbiddenException();

        var now = _clock.UtcNow;
        var packageId = PackageId.New();
        var reference = await GenerateUniqueReference(now, cancellationToken);
        Package package;

        if (command.ActorRole == "OrganizationAdmin")
        {
            var admin = await _adminRepository.GetByIdAsync(
                new OrganizationAdminAccountId(command.ActorId),
                cancellationToken);
            if (admin is null
                || admin.OrganizationId != organizationId
                || admin.Status != OrganizationAdminAccountStatus.Active)
                throw new PickupCreationForbiddenException();

            package = Package.CreateByOrganizationAdmin(
                packageId, organizationId, recipient.Id, admin.Id, stationId, reference, command.Description, now);
        }
        else if (command.ActorRole == "Employee")
        {
            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeAccountId(command.ActorId),
                cancellationToken);
            if (employee is null
                || employee.OrganizationId != organizationId
                || employee.Status != EmployeeAccountStatus.Active
                || !employee.CanManageRecords)
                throw new PickupCreationForbiddenException();

            package = Package.CreateByEmployee(
                packageId, organizationId, recipient.Id, employee.Id, stationId, reference, command.Description, now);
        }
        else
        {
            throw new PickupCreationForbiddenException();
        }

        await _packageRepository.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PickupResponse(
            package.Id.Value,
            package.Reference,
            package.Description,
            $"{recipient.FirstName} {recipient.LastName}",
            FormatLocation(claim),
            package.Status.ToString(),
            package.DeadlineAt,
            package.CreatedAt,
            true,
            true);
    }

    private async Task<string> GenerateUniqueReference(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
        {
            var reference = _referenceGenerator.Generate(now);
            if (!await _packageRepository.ExistsByReferenceAsync(reference, cancellationToken))
                return reference;
        }

        throw new InvalidOperationException("Could not generate a unique package reference.");
    }

    private static string FormatLocation(StationClaim claim)
        => $"{claim.Location.Address} {claim.Location.HouseNumber}, {claim.Location.ZipCode} {claim.Location.City}";
}

public sealed class PickupStationForbiddenException()
    : Exception("Pickup station does not belong to the organization.");

public sealed class PickupCreationForbiddenException()
    : Exception("The current user cannot create pickups.");
