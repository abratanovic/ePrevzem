using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations.Dtos;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.Create;

public sealed record CreateOrganizationCommand(
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    TimeSpan DefaultPickupDuration) : IRequest<OrganizationResponse>;

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

        var org = Organization.Create(
            OrganizationId.New(),
            command.Name,
            command.TaxNumber,
            command.RegistrationNumber,
            command.DefaultPickupDuration,
            now);

        await _organizationRepository.AddAsync(org, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrganizationResponse(
            org.Id.Value,
            org.Name,
            org.TaxNumber,
            org.RegistrationNumber,
            org.DefaultPickupDuration,
            org.CreatedAt);
    }
}
