using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Persistence;

public class EPrevzemDbContext : DbContext, IUnitOfWork
{
    public DbSet<SystemAdmin> SystemAdmins { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<OrganizationAdminAccount> OrganizationAdminAccounts { get; set; } = null!;
    public DbSet<EmployeeAccount> EmployeeAccounts { get; set; } = null!;
    public DbSet<ProvisioningCode> ProvisioningCodes { get; set; } = null!;
    public DbSet<CitizenUser> CitizenUsers { get; set; } = null!;
    public DbSet<CitizenActivationCode> CitizenActivationCodes { get; set; } = null!;
    public DbSet<PickupStation> PickupStations { get; set; } = null!;
    public DbSet<Locker> Lockers { get; set; } = null!;
    public DbSet<StationClaim> StationClaims { get; set; } = null!;

    public EPrevzemDbContext(DbContextOptions<EPrevzemDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EPrevzemDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
