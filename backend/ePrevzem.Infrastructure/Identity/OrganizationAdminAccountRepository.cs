using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class OrganizationAdminAccountRepository : IOrganizationAdminAccountRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public OrganizationAdminAccountRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default)
        => _dbContext.OrganizationAdminAccounts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _dbContext.OrganizationAdminAccounts.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _dbContext.OrganizationAdminAccounts.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default)
        => _dbContext.OrganizationAdminAccounts.AddAsync(account, cancellationToken).AsTask();
}
