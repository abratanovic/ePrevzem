using ePrevzem.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Persistence;

public class EPrevzemDbContext : DbContext, IEPrevzemDbContext
{
    public EPrevzemDbContext(DbContextOptions<EPrevzemDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EPrevzemDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
