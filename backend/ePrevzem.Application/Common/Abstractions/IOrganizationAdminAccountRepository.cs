using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface IOrganizationAdminAccountRepository
{
    Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default);
    Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default);
}
