using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Audit;

public class AuditLogEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_with_employee_actor_constructs_entry()
    {
        var id = AuditLogEntryId.New();
        var employee = EmployeeAccountId.New();
        var package = PackageId.New();
        var org = OrganizationId.New();

        var entry = AuditLogEntry.Record(
            id,
            Now,
            actorKind: AuditActorKind.Employee,
            actorCitizenUserId: null,
            actorEmployeeAccountId: employee,
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId: org,
            action: AuditAction.PackagePlaced,
            targetKind: AuditTargetKind.Package,
            targetId: package.Value,
            details: """{"lockerNumber":3}""");

        entry.Id.Should().Be(id);
        entry.OccurredAt.Should().Be(Now);
        entry.ActorKind.Should().Be(AuditActorKind.Employee);
        entry.ActorEmployeeAccountId.Should().Be(employee);
        entry.OrganizationId.Should().Be(org);
        entry.Action.Should().Be(AuditAction.PackagePlaced);
        entry.TargetKind.Should().Be(AuditTargetKind.Package);
        entry.TargetId.Should().Be(package.Value);
        entry.Details.Should().Be("""{"lockerNumber":3}""");
    }

    [Fact]
    public void Record_with_system_actor_allows_no_actor_ids()
    {
        var entry = AuditLogEntry.Record(
            AuditLogEntryId.New(), Now,
            AuditActorKind.System, null, null, null, null, null,
            AuditAction.PackageExpired, AuditTargetKind.Package, Guid.NewGuid(), null);

        entry.ActorKind.Should().Be(AuditActorKind.System);
        entry.ActorCitizenUserId.Should().BeNull();
        entry.ActorEmployeeAccountId.Should().BeNull();
        entry.ActorSystemAdminId.Should().BeNull();
    }

    [Theory]
    [InlineData(AuditActorKind.Citizen)]
    [InlineData(AuditActorKind.Employee)]
    [InlineData(AuditActorKind.OrganizationAdmin)]
    [InlineData(AuditActorKind.SystemAdmin)]
    public void Record_non_system_actor_without_matching_id_throws(AuditActorKind kind)
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, kind, null, null, null, null, null,
            AuditAction.PackagePlaced, AuditTargetKind.Package, Guid.NewGuid(), null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_non_system_actor_with_multiple_actor_ids_throws()
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, AuditActorKind.Employee,
            actorCitizenUserId: CitizenUserId.New(),
            actorEmployeeAccountId: EmployeeAccountId.New(),
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId: null,
            action: AuditAction.PackagePlaced,
            targetKind: AuditTargetKind.Package,
            targetId: Guid.NewGuid(),
            details: null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_system_actor_with_actor_id_throws()
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, AuditActorKind.System,
            actorCitizenUserId: CitizenUserId.New(),
            actorEmployeeAccountId: null,
            actorOrganizationAdminAccountId: null,
            actorSystemAdminId: null,
            organizationId: null,
            action: AuditAction.PackageExpired,
            targetKind: AuditTargetKind.Package,
            targetId: Guid.NewGuid(),
            details: null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_with_organization_admin_actor_constructs_entry()
    {
        var admin = OrganizationAdminAccountId.New();

        var entry = AuditLogEntry.Record(
            AuditLogEntryId.New(),
            Now,
            AuditActorKind.OrganizationAdmin,
            actorCitizenUserId: null,
            actorEmployeeAccountId: null,
            actorOrganizationAdminAccountId: admin,
            actorSystemAdminId: null,
            organizationId: OrganizationId.New(),
            action: AuditAction.PackageCreated,
            targetKind: AuditTargetKind.Package,
            targetId: Guid.NewGuid(),
            details: null);

        entry.ActorKind.Should().Be(AuditActorKind.OrganizationAdmin);
        entry.ActorOrganizationAdminAccountId.Should().Be(admin);
    }
}
