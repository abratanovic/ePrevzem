using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ICitizenUserRepository
{
    Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default);
    Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken cancellationToken = default);
    Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default);
}
