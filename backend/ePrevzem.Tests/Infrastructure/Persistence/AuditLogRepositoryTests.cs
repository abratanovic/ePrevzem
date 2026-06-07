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
    public async Task GetForOrganizationAsync_enriches_organization_admin_actor_display()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var orgId = OrganizationId.New();
        var adminId = OrganizationAdminAccountId.New();
        var admin = OrganizationAdminAccount.Create(
            adminId,
            orgId,
            "Ana",
            "Novak",
            "ana.novak@example.test",
            "hash",
            Now);

        db.OrganizationAdminAccounts.Add(admin);
        await repository.AddAsync(Entry(
            orgId,
            AuditActorKind.OrganizationAdmin,
            actorOrganizationAdminAccountId: adminId,
            action: AuditAction.OrganizationAdminLoggedIn,
            occurredAt: Now));
        await db.SaveChangesAsync();

        var result = await repository.GetForOrganizationAsync(
            orgId,
            new AuditLogQueryFilter(50, null, null, null, null, null));

        result.Should().ContainSingle();
        result[0].ActorDisplayName.Should().Be("Ana Novak");
        result[0].ActorEmail.Should().Be("ana.novak@example.test");
    }

    [Fact]
    public async Task GetForOrganizationAsync_enriches_employee_actor_display()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var orgId = OrganizationId.New();
        var employeeId = EmployeeAccountId.New();
        var employee = EmployeeAccount.Create(
            employeeId,
            orgId,
            "Boris",
            "Kranjc",
            "boris.kranjc@example.test",
            [EmployeeAccountRole.Operator],
            [],
            ProvisioningCodeId.New(),
            Now);

        db.EmployeeAccounts.Add(employee);
        await repository.AddAsync(Entry(
            orgId,
            AuditActorKind.Employee,
            actorEmployeeAccountId: employeeId,
            action: AuditAction.PackageCreated,
            occurredAt: Now));
        await db.SaveChangesAsync();

        var result = await repository.GetForOrganizationAsync(
            orgId,
            new AuditLogQueryFilter(50, null, null, null, null, null));

        result.Should().ContainSingle();
        result[0].ActorDisplayName.Should().Be("Boris Kranjc");
        result[0].ActorEmail.Should().Be("boris.kranjc@example.test");
    }

    [Fact]
    public async Task GetForOrganizationAsync_filters_by_actor_kind_and_id()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var org = OrganizationId.New();
        var otherOrg = OrganizationId.New();
        var employeeId = EmployeeAccountId.New();
        var adminId = OrganizationAdminAccountId.New();
        var citizenId = CitizenUserId.New();

        await repository.AddAsync(Entry(
            org,
            AuditActorKind.Employee,
            actorEmployeeAccountId: employeeId,
            action: AuditAction.PackageCreated,
            occurredAt: Now));
        await repository.AddAsync(Entry(
            org,
            AuditActorKind.OrganizationAdmin,
            actorOrganizationAdminAccountId: adminId,
            action: AuditAction.OrganizationAdminLoggedIn,
            occurredAt: Now.AddMinutes(1)));
        await repository.AddAsync(Entry(
            org,
            AuditActorKind.Citizen,
            actorCitizenUserId: citizenId,
            action: AuditAction.PackagePickedUpByCitizen,
            occurredAt: Now.AddMinutes(2)));
        await repository.AddAsync(Entry(
            otherOrg,
            AuditActorKind.Employee,
            actorEmployeeAccountId: employeeId,
            action: AuditAction.PackageCancelled,
            occurredAt: Now.AddMinutes(3)));
        await db.SaveChangesAsync();

        var employeeResult = await repository.GetForOrganizationAsync(
            org,
            new AuditLogQueryFilter(
                50, null, null, null, null, null,
                ActorKind: AuditActorKind.Employee,
                ActorId: employeeId.Value));
        var adminResult = await repository.GetForOrganizationAsync(
            org,
            new AuditLogQueryFilter(
                50, null, null, null, null, null,
                ActorKind: AuditActorKind.OrganizationAdmin,
                ActorId: adminId.Value));
        var citizenResult = await repository.GetForOrganizationAsync(
            org,
            new AuditLogQueryFilter(
                50, null, null, null, null, null,
                ActorKind: AuditActorKind.Citizen,
                ActorId: citizenId.Value));

        employeeResult.Should().ContainSingle();
        employeeResult[0].ActorEmployeeAccountId.Should().Be(employeeId.Value);
        employeeResult[0].Action.Should().Be(nameof(AuditAction.PackageCreated));

        adminResult.Should().ContainSingle();
        adminResult[0].ActorOrganizationAdminAccountId.Should().Be(adminId.Value);

        citizenResult.Should().ContainSingle();
        citizenResult[0].ActorCitizenUserId.Should().Be(citizenId.Value);
    }

    [Fact]
    public async Task GetActorOptionsForOrganizationAsync_returns_distinct_non_system_actors_with_display()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var org = OrganizationId.New();
        var employeeId = EmployeeAccountId.New();
        var adminId = OrganizationAdminAccountId.New();

        db.EmployeeAccounts.Add(EmployeeAccount.Create(
            employeeId,
            org,
            "Boris",
            "Kranjc",
            "boris.kranjc@example.test",
            [EmployeeAccountRole.Operator],
            [],
            ProvisioningCodeId.New(),
            Now));
        db.OrganizationAdminAccounts.Add(OrganizationAdminAccount.Create(
            adminId,
            org,
            "Ana",
            "Novak",
            "ana.novak@example.test",
            "hash",
            Now));

        await repository.AddAsync(Entry(
            org,
            AuditActorKind.Employee,
            actorEmployeeAccountId: employeeId,
            action: AuditAction.PackageCreated,
            occurredAt: Now));
        await repository.AddAsync(Entry(
            org,
            AuditActorKind.Employee,
            actorEmployeeAccountId: employeeId,
            action: AuditAction.PackagePlaced,
            occurredAt: Now.AddMinutes(1)));
        await repository.AddAsync(Entry(
            org,
            AuditActorKind.OrganizationAdmin,
            actorOrganizationAdminAccountId: adminId,
            action: AuditAction.OrganizationAdminLoggedIn,
            occurredAt: Now.AddMinutes(2)));
        await repository.AddAsync(Entry(
            org,
            AuditActorKind.System,
            action: AuditAction.PackageExpired,
            occurredAt: Now.AddMinutes(3)));
        await db.SaveChangesAsync();

        var result = await repository.GetActorOptionsForOrganizationAsync(org);

        result.Should().HaveCount(2);
        result.Should().Contain(x =>
            x.ActorKind == nameof(AuditActorKind.Employee)
            && x.ActorId == employeeId.Value
            && x.DisplayName == "Boris Kranjc"
            && x.Email == "boris.kranjc@example.test");
        result.Should().Contain(x =>
            x.ActorKind == nameof(AuditActorKind.OrganizationAdmin)
            && x.ActorId == adminId.Value
            && x.DisplayName == "Ana Novak"
            && x.Email == "ana.novak@example.test");
    }

    [Fact]
    public async Task GetForOrganizationAsync_keeps_entry_when_actor_account_is_missing()
    {
        await using var db = CreateContext();
        var repository = new AuditLogRepository(db);
        var orgId = OrganizationId.New();

        await repository.AddAsync(Entry(
            orgId,
            AuditActorKind.Employee,
            actorEmployeeAccountId: EmployeeAccountId.New(),
            action: AuditAction.PackageCreated,
            occurredAt: Now));
        await db.SaveChangesAsync();

        var result = await repository.GetForOrganizationAsync(
            orgId,
            new AuditLogQueryFilter(50, null, null, null, null, null));

        result.Should().ContainSingle();
        result[0].ActorKind.Should().Be(nameof(AuditActorKind.Employee));
        result[0].ActorDisplayName.Should().BeNull();
        result[0].ActorEmail.Should().BeNull();

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
        => Entry(
            organizationId,
            AuditActorKind.Employee,
            actorEmployeeAccountId: EmployeeAccountId.New(),
            action: action,
            occurredAt: occurredAt);

    private static AuditLogEntry Entry(
        OrganizationId organizationId,
        AuditActorKind actorKind,
        CitizenUserId? actorCitizenUserId = null,
        EmployeeAccountId? actorEmployeeAccountId = null,
        OrganizationAdminAccountId? actorOrganizationAdminAccountId = null,
        SystemAdminId? actorSystemAdminId = null,
        AuditAction action = AuditAction.PackageCreated,
        DateTimeOffset? occurredAt = null)
        => AuditLogEntry.Record(
            AuditLogEntryId.New(),
            occurredAt ?? Now,
            actorKind,
            actorCitizenUserId,
            actorEmployeeAccountId,
            actorOrganizationAdminAccountId,
            actorSystemAdminId,
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
