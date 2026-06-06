using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
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

    private static EPrevzemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EPrevzemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPrevzemDbContext(options);
    }
}
