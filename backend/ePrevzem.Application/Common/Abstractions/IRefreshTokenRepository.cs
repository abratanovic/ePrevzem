using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
