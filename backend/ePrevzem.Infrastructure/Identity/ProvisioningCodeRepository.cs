using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class ProvisioningCodeRepository : IProvisioningCodeRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public ProvisioningCodeRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProvisioningCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dbContext.ProvisioningCodes.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task AddAsync(ProvisioningCode provisioningCode, CancellationToken cancellationToken = default)
        => _dbContext.ProvisioningCodes.AddAsync(provisioningCode, cancellationToken).AsTask();
}
