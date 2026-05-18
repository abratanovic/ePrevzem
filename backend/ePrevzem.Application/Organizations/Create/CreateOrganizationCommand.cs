using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations.Dtos;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.Create;

public sealed record CreateOrganizationCommand(
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    int DefaultPickupDurationDays) : IRequest<OrganizationResponse>;

public sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, OrganizationResponse>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<OrganizationResponse> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        if (await _organizationRepository.ExistsByTaxNumberAsync(command.TaxNumber, cancellationToken))
            throw new DuplicateTaxNumberException(command.TaxNumber);

        if (await _organizationRepository.ExistsByRegistrationNumberAsync(command.RegistrationNumber, cancellationToken))
            throw new DuplicateRegistrationNumberException(command.RegistrationNumber);

        var org = Organization.Create(
            OrganizationId.New(),
            command.Name,
            command.TaxNumber,
            command.RegistrationNumber,
            TimeSpan.FromDays(command.DefaultPickupDurationDays),
            now);

        await _organizationRepository.AddAsync(org, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrganizationResponse(
            org.Id.Value,
            org.Name,
            org.TaxNumber,
            org.RegistrationNumber,
            (int)org.DefaultPickupDuration.TotalDays,
            org.CreatedAt);
    }
}

public sealed class DuplicateTaxNumberException(string taxNumber)
    : Exception($"An organization with tax number '{taxNumber}' already exists.");

public sealed class DuplicateRegistrationNumberException(string registrationNumber)
    : Exception($"An organization with registration number '{registrationNumber}' already exists.");
