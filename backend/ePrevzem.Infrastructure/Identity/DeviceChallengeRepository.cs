using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class DeviceChallengeRepository : IDeviceChallengeRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public DeviceChallengeRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(DeviceChallenge challenge, CancellationToken cancellationToken = default)
        => _dbContext.DeviceChallenges.AddAsync(challenge, cancellationToken).AsTask();

    public Task<DeviceChallenge?> GetLatestActiveAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => _dbContext.DeviceChallenges
            .Where(x => x.DeviceId == deviceId && x.ConsumedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
