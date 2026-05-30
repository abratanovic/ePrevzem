using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
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
            .Include(x => x.Roles)
            .Include(x => x.StationAccess)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => _dbContext.EmployeeAccounts
            .Include(x => x.Roles)
            .Include(x => x.StationAccess)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
