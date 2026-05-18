using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class SystemAdminRepository : ISystemAdminRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public SystemAdminRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SystemAdmin?> GetByIdAsync(SystemAdminId id, CancellationToken cancellationToken = default)
        => _dbContext.SystemAdmins.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SystemAdmin?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        => _dbContext.SystemAdmins.SingleOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => _dbContext.SystemAdmins.AnyAsync(cancellationToken);

    public Task AddAsync(SystemAdmin systemAdmin, CancellationToken cancellationToken = default)
        => _dbContext.SystemAdmins.AddAsync(systemAdmin, cancellationToken).AsTask();
}
