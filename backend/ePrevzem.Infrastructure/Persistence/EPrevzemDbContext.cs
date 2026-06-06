using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Persistence;

public class EPrevzemDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private bool _isDispatchingDomainEvents;

    public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;
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
    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<Placement> Placements { get; set; } = null!;

    public EPrevzemDbContext(
        DbContextOptions<EPrevzemDbContext> options,
        IDomainEventDispatcher? domainEventDispatcher = null) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EPrevzemDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuditLogIsAppendOnly();

        if (_domainEventDispatcher is null || _isDispatchingDomainEvents)
            return await base.SaveChangesAsync(cancellationToken);

        var domainEvents = CollectDomainEvents();
        if (domainEvents.Count == 0)
            return await base.SaveChangesAsync(cancellationToken);

        if (Database.CurrentTransaction is not null)
            return await SaveChangesAndDispatchDomainEventsAsync(domainEvents, cancellationToken);

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        var result = await SaveChangesAndDispatchDomainEventsAsync(domainEvents, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<int> SaveChangesAndDispatchDomainEventsAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        _isDispatchingDomainEvents = true;
        try
        {
            await _domainEventDispatcher!.DispatchAsync(domainEvents, cancellationToken);
            await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _isDispatchingDomainEvents = false;
        }

        return result;
    }

    private IReadOnlyCollection<IDomainEvent> CollectDomainEvents()
    {
        var aggregateRoots = ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var aggregateRoot in aggregateRoots)
            aggregateRoot.ClearDomainEvents();

        return domainEvents;
    }

    private void EnsureAuditLogIsAppendOnly()
    {
        var invalidAuditChanges = ChangeTracker
            .Entries<AuditLogEntry>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);

        if (invalidAuditChanges)
            throw new InvalidOperationException("Audit log entries are append-only.");
    }
}
