using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Persistence;

public class EPrevzemDbContext : DbContext, IUnitOfWork
{
    public DbSet<SystemAdmin> SystemAdmins { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public EPrevzemDbContext(DbContextOptions<EPrevzemDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EPrevzemDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
