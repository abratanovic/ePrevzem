using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface IProvisioningCodeRepository
{
    Task<ProvisioningCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(ProvisioningCode provisioningCode, CancellationToken cancellationToken = default);
}
