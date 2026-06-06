using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Audit.Persistence;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries", table =>
            table.HasCheckConstraint(
                "ck_audit_log_entries_actor",
                """
                (
                    actor_kind = 'System'
                    AND actor_citizen_user_id IS NULL
                    AND actor_employee_account_id IS NULL
                    AND actor_organization_admin_account_id IS NULL
                    AND actor_system_admin_id IS NULL
                )
                OR (
                    actor_kind = 'Citizen'
                    AND actor_citizen_user_id IS NOT NULL
                    AND actor_employee_account_id IS NULL
                    AND actor_organization_admin_account_id IS NULL
                    AND actor_system_admin_id IS NULL
                )
                OR (
                    actor_kind = 'Employee'
                    AND actor_citizen_user_id IS NULL
                    AND actor_employee_account_id IS NOT NULL
                    AND actor_organization_admin_account_id IS NULL
                    AND actor_system_admin_id IS NULL
                )
                OR (
                    actor_kind = 'OrganizationAdmin'
                    AND actor_citizen_user_id IS NULL
                    AND actor_employee_account_id IS NULL
                    AND actor_organization_admin_account_id IS NOT NULL
                    AND actor_system_admin_id IS NULL
                )
                OR (
                    actor_kind = 'SystemAdmin'
                    AND actor_citizen_user_id IS NULL
                    AND actor_employee_account_id IS NULL
                    AND actor_organization_admin_account_id IS NULL
                    AND actor_system_admin_id IS NOT NULL
                )
                """));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new AuditLogEntryId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ActorKind)
            .HasColumnName("actor_kind")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ActorCitizenUserId)
            .HasColumnName("actor_citizen_user_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new CitizenUserId(x.Value) : null);

        builder.Property(x => x.ActorEmployeeAccountId)
            .HasColumnName("actor_employee_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new EmployeeAccountId(x.Value) : null);

        builder.Property(x => x.ActorOrganizationAdminAccountId)
            .HasColumnName("actor_organization_admin_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new OrganizationAdminAccountId(x.Value) : null);

        builder.Property(x => x.ActorSystemAdminId)
            .HasColumnName("actor_system_admin_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new SystemAdminId(x.Value) : null);

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new OrganizationId(x.Value) : null);

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.TargetKind)
            .HasColumnName("target_kind")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(x => x.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.OrganizationId, x.OccurredAt });
        builder.HasIndex(x => x.ActorCitizenUserId);
        builder.HasIndex(x => x.ActorEmployeeAccountId);
        builder.HasIndex(x => x.ActorOrganizationAdminAccountId);
        builder.HasIndex(x => x.ActorSystemAdminId);
        builder.HasIndex(x => new { x.TargetKind, x.TargetId });
        builder.HasIndex(x => x.Action);

        builder.Ignore(x => x.DomainEvents);
    }
}
