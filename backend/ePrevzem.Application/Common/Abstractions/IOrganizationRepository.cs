using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Common.Abstractions;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
}
