using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Application.Common.Abstractions;

public interface IEPrevzemDbContext
{
    DbSet<SystemAdmin> SystemAdmins { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
