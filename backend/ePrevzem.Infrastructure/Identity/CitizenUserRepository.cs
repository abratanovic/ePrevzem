using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class CitizenUserRepository : ICitizenUserRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public CitizenUserRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CitizenUser?> GetByIdAsync(CitizenUserId id, CancellationToken cancellationToken = default)
        => _dbContext.CitizenUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default)
        => _dbContext.CitizenUsers.SingleOrDefaultAsync(x => x.Emso == emso, cancellationToken);

    public Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken cancellationToken = default)
        => _dbContext.CitizenUsers
            .Include(x => x.Devices)
            .FirstOrDefaultAsync(u => u.Devices.Any(d => d.Id == deviceId), cancellationToken);

    public Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default)
        => _dbContext.CitizenUsers.AddAsync(user, cancellationToken).AsTask();
}
