using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class EmployeeAccountRepository : IEmployeeAccountRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public EmployeeAccountRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _dbContext.EmployeeAccounts
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => _dbContext.EmployeeAccounts
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default)
        => _dbContext.EmployeeAccounts
            .Include(x => x.Devices)
            .FirstOrDefaultAsync(a => a.Devices.Any(d => d.Id == deviceId), cancellationToken);

    public Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default)
        => _dbContext.EmployeeAccounts.AddAsync(account, cancellationToken).AsTask();

    public async Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default)
        => await _dbContext.EmployeeAccounts
            .Where(x => x.OrganizationId == organisationId)
            .ToListAsync(cancellationToken);
}
