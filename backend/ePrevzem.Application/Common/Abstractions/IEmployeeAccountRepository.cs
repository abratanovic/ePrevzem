using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Common.Abstractions;

public interface IEmployeeAccountRepository
{
    Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default);
    Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default);
    Task<EmployeeAccount?> GetByProvisioningCodeIdAsync(ProvisioningCodeId provisioningCodeId, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default);
}
