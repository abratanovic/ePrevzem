using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public RefreshTokenRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
        => _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        => _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
}
