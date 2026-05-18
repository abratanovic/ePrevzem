using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ISystemAdminRepository
{
    Task<SystemAdmin?> GetByIdAsync(SystemAdminId id, CancellationToken cancellationToken = default);
    Task<SystemAdmin?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SystemAdmin systemAdmin, CancellationToken cancellationToken = default);
}
