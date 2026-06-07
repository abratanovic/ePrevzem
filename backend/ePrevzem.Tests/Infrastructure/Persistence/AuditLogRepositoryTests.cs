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

namespace ePrevzem.Tests.Infrastructure.Persistence;

public class AuditLogRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetForOrganizationAsync_returns_only_requested_organization_entries()
    {
        await using var db = CreateContext();
        var orgId = OrganizationId.New();
        var otherOrgId = OrganizationId.New();
        var repository = new AuditLogRepository(db);

        await repository.AddAsync(Entry(orgId, AuditAction.PackageCreated, Now));
        await repository.AddAsync(Entry(otherOrgId, AuditAction.PackageCancelled, Now.AddMinutes(1)));
        await db.SaveChangesAsync();

        var result = await repository.GetForOrganizationAsync(
            orgId,
            new AuditLogQueryFilter(50, null, null, null, null, null));

        result.Should().ContainSingle();
        result[0].OrganizationId.Should().Be(orgId.Value);
        result[0].Action.Should().Be(nameof(AuditAction.PackageCreated));
    }

    [Fact]
    public async Task GetForAdminAsync_applies_action_target_and_limit_filters()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var orgId = OrganizationId.New();

        await repository.AddAsync(Entry(orgId, AuditAction.PackageCreated, Now));
        await repository.AddAsync(Entry(orgId, AuditAction.PackageCancelled, Now.AddMinutes(1)));
        await repository.AddAsync(Entry(orgId, AuditAction.PackageCancelled, Now.AddMinutes(2)));
        await db.SaveChangesAsync();

        var result = await repository.GetForAdminAsync(
            new AuditLogQueryFilter(
                1,
                null,
                null,
                orgId,
                AuditAction.PackageCancelled,
                AuditTargetKind.Package));

        result.Should().ContainSingle();
        result[0].Action.Should().Be(nameof(AuditAction.PackageCancelled));
        result[0].OccurredAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public async Task GetForCitizenAsync_returns_own_actor_events_and_events_about_their_packages_only()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var org = OrganizationId.New();
        var citizen = CitizenUserId.New();
        var otherCitizen = CitizenUserId.New();

        var myPackage = PackageFor(citizen, org);
        var otherPackage = PackageFor(otherCitizen, org);
        db.Packages.Add(myPackage);
        db.Packages.Add(otherPackage);
        await db.SaveChangesAsync();

        // Citizen's own action — returned.
        await repository.AddAsync(CitizenActorEntry(
            citizen, AuditAction.PackagePickedUpByCitizen, myPackage.Id.Value, Now));
        // Employee placed the citizen's package — returned (about my package).
        await repository.AddAsync(PackageEventEntry(
            AuditAction.PackagePlaced, myPackage.Id.Value, org, Now.AddMinutes(1)));
        // Employee placed another citizen's package — excluded.
        await repository.AddAsync(PackageEventEntry(
            AuditAction.PackagePlaced, otherPackage.Id.Value, org, Now.AddMinutes(2)));
        // Citizen's own action but outside the whitelist — excluded.
        await repository.AddAsync(CitizenActorEntry(
            citizen, AuditAction.CitizenActivationCodeIssued, Guid.NewGuid(), Now.AddMinutes(3)));
        await db.SaveChangesAsync();

        var result = await repository.GetForCitizenAsync(
            citizen,
            new AuditLogQueryFilter(
                50, null, null, null, null, null,
                ActionsIn: new[] { AuditAction.PackagePickedUpByCitizen, AuditAction.PackagePlaced }));

        result.Should().HaveCount(2);
        result.Select(x => x.Action).Should().BeEquivalentTo(
            new[] { nameof(AuditAction.PackagePlaced), nameof(AuditAction.PackagePickedUpByCitizen) });
    }

    [Fact]
    public async Task GetForOrganizationAsync_scopes_to_one_employee_and_whitelisted_actions()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var org = OrganizationId.New();
        var operatorId = EmployeeAccountId.New();
        var otherEmployee = EmployeeAccountId.New();

        // Mine + whitelisted — returned.
        await repository.AddAsync(EmployeeActorEntry(operatorId, org, AuditAction.PackagePlaced, Now));
        // Mine but not whitelisted — excluded.
        await repository.AddAsync(EmployeeActorEntry(operatorId, org, AuditAction.EmployeeAccountLoggedIn, Now.AddMinutes(1)));
        // Whitelisted but another employee — excluded.
        await repository.AddAsync(EmployeeActorEntry(otherEmployee, org, AuditAction.PackagePlaced, Now.AddMinutes(2)));
        await db.SaveChangesAsync();

        var result = await repository.GetForOrganizationAsync(
            org,
            new AuditLogQueryFilter(
                50, null, null, null, null, null,
                ActorEmployeeAccountId: operatorId,
                ActionsIn: new[] { AuditAction.PackagePlaced, AuditAction.PackageRemovedByEmployee }));

        result.Should().ContainSingle();
        result[0].Action.Should().Be(nameof(AuditAction.PackagePlaced));
        result[0].ActorEmployeeAccountId.Should().Be(operatorId.Value);
    }

    [Fact]
    public async Task SaveChanges_rejects_modified_audit_entries()
    {
        await using var db = CreateContext();
        var entry = Entry(OrganizationId.New(), AuditAction.PackageCreated, Now);
        db.AuditLogEntries.Add(entry);
        await db.SaveChangesAsync();

        db.Entry(entry).Property(x => x.Details).CurrentValue = """{"changed":true}""";

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    private static AuditLogEntry Entry(OrganizationId organizationId, AuditAction action, DateTimeOffset occurredAt)
        => AuditLogEntry.Record(
            AuditLogEntryId.New(),
            occurredAt,
            AuditActorKind.Employee,
            actorCitizenUserId: null,
            actorEmployeeAccountId: EmployeeAccountId.New(),
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId,
            action,
            AuditTargetKind.Package,
            Guid.NewGuid(),
            details: null);

    private static AuditLogEntry CitizenActorEntry(
        CitizenUserId citizen, AuditAction action, Guid targetId, DateTimeOffset occurredAt)
        => AuditLogEntry.Record(
            AuditLogEntryId.New(), occurredAt, AuditActorKind.Citizen,
            actorCitizenUserId: citizen,
            actorEmployeeAccountId: null,
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId: null,
            action, AuditTargetKind.Package, targetId, details: null);

    private static AuditLogEntry PackageEventEntry(
        AuditAction action, Guid packageId, OrganizationId organizationId, DateTimeOffset occurredAt)
        => AuditLogEntry.Record(
            AuditLogEntryId.New(), occurredAt, AuditActorKind.Employee,
            actorCitizenUserId: null,
            actorEmployeeAccountId: EmployeeAccountId.New(),
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId,
            action, AuditTargetKind.Package, packageId, details: null);

    private static AuditLogEntry EmployeeActorEntry(
        EmployeeAccountId employee, OrganizationId organizationId, AuditAction action, DateTimeOffset occurredAt)
        => AuditLogEntry.Record(
            AuditLogEntryId.New(), occurredAt, AuditActorKind.Employee,
            actorCitizenUserId: null,
            actorEmployeeAccountId: employee,
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId,
            action, AuditTargetKind.Package, Guid.NewGuid(), details: null);

    private static Package PackageFor(CitizenUserId recipient, OrganizationId organizationId)
        => Package.CreateByEmployee(
            PackageId.New(),
            organizationId,
            recipient,
            EmployeeAccountId.New(),
            PickupStationId.New(),
            $"EP-{Guid.NewGuid():N}".Substring(0, 10),
            "Diploma",
            Now);

    private static EPrevzemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EPrevzemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPrevzemDbContext(options);
    }
}
