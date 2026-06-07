using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface IDeviceChallengeRepository
{
    Task AddAsync(DeviceChallenge challenge, CancellationToken cancellationToken = default);
    Task<DeviceChallenge?> GetLatestActiveAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
