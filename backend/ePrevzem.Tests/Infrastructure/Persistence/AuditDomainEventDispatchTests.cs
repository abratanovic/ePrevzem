using ePrevzem.Application;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Audit;
using ePrevzem.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ePrevzem.Tests.Infrastructure.Persistence;

public class AuditDomainEventDispatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_dispatches_domain_events_and_persists_audit_entries()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EPrevzemDbContext>();

        var organization = Organization.Create(
            OrganizationId.New(),
            "Univerza v Ljubljani",
            "12345678",
            "87654321",
            TimeSpan.FromDays(5),
            Now);
        organization.ClearDomainEvents();

        var station = PickupStation.Create(PickupStationId.New(), "EP-PM-001", Now);
        var locker = station.AddLocker(LockerId.New(), 1);
        station.ClearDomainEvents();
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            station.Id,
            organization.Id,
            Location.Create(46m, 14m, "Kongresni trg", "1", "1000", "Ljubljana"),
            Now);
        claim.ClearDomainEvents();

        var citizen = CitizenUser.Onboard(
            CitizenUserId.New(),
            "Ana",
            "Kovač",
            "0101006500006",
            null,
            null,
            Now);
        citizen.ClearDomainEvents();

        db.Organizations.Add(organization);
        db.PickupStations.Add(station);
        db.StationClaims.Add(claim);
        db.CitizenUsers.Add(citizen);
        await db.SaveChangesAsync();

        var createdPackage = Package.CreateByEmployee(
            PackageId.New(),
            organization.Id,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000123",
            "Osebna izkaznica",
            Now.AddMinutes(1));

        db.Packages.Add(createdPackage);
        await db.SaveChangesAsync();

        var cancelledPackage = Package.CreateByEmployee(
            PackageId.New(),
            organization.Id,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000124",
            "Potrdilo",
            Now.AddMinutes(2));
        cancelledPackage.ClearDomainEvents();
        db.Packages.Add(cancelledPackage);
        await db.SaveChangesAsync();

        cancelledPackage.Cancel(EmployeeAccountId.New(), Now.AddMinutes(3));
        await db.SaveChangesAsync();

        var deletedPackage = Package.CreateByEmployee(
            PackageId.New(),
            organization.Id,
            citizen.Id,
            EmployeeAccountId.New(),
            station.Id,
            "EP-2026-000125",
            "Izpisek",
            Now.AddMinutes(4));
        deletedPackage.ClearDomainEvents();
        db.Packages.Add(deletedPackage);
        await db.SaveChangesAsync();

        deletedPackage.MarkDeleted(EmployeeAccountId.New(), Now.AddMinutes(5));
        db.Packages.Remove(deletedPackage);
        await db.SaveChangesAsync();

        station.SetLockerServiceability(locker.Id, false, Now.AddMinutes(6));
        await db.SaveChangesAsync();

        var entries = await db.AuditLogEntries.AsNoTracking().ToListAsync();
        entries.Select(x => x.Action).Should().Contain([
            AuditAction.PackageCreated,
            AuditAction.PackageCancelled,
            AuditAction.PackageDeleted,
            AuditAction.LockerServiceabilityChanged
        ]);

        var entry = entries.Should().ContainSingle(x => x.Action == AuditAction.PackageCreated).Subject;
        entry.Action.Should().Be(AuditAction.PackageCreated);
        entry.ActorKind.Should().Be(AuditActorKind.Employee);
        entry.OrganizationId.Should().Be(organization.Id);
        entry.TargetKind.Should().Be(AuditTargetKind.Package);
        entry.TargetId.Should().Be(createdPackage.Id.Value);
        entry.Details.Should().Contain("Osebna izkaznica");
        entry.Details.Should().Contain("Univerza v Ljubljani");

        entries.Should().ContainSingle(x =>
            x.Action == AuditAction.PackageDeleted
            && x.TargetId == deletedPackage.Id.Value
            && x.OrganizationId == organization.Id);
        entries.Should().ContainSingle(x =>
            x.Action == AuditAction.LockerServiceabilityChanged
            && x.TargetId == locker.Id.Value
            && x.OrganizationId == organization.Id);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApplication();
        services.AddDbContext<EPrevzemDbContext>(options => options
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EPrevzemDbContext>());
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditContextLookup, AuditContextLookup>();
        services.AddScoped<ICurrentUser, TestAuditCurrentUser>();

        return services.BuildServiceProvider();
    }
}

public sealed class TestAuditCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public Guid? OrganizationId => null;
    public bool IsAuthenticated => false;
    public bool IsInRole(string role) => false;
}
