using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", t =>
            t.HasCheckConstraint(
                "CK_refresh_tokens_single_actor",
                "(system_admin_id IS NOT NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NOT NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NOT NULL)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new RefreshTokenId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.SystemAdminId)
            .HasColumnName("system_admin_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new SystemAdminId(x.Value) : null);

        builder.Property(x => x.OrganizationAdminAccountId)
            .HasColumnName("organization_admin_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new OrganizationAdminAccountId(x.Value) : null);

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new RefreshTokenId(x.Value) : null);

        builder.HasOne<SystemAdmin>()
            .WithMany()
            .HasForeignKey(x => x.SystemAdminId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.EmployeeAccountId)
            .HasColumnName("employee_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new EmployeeAccountId(x.Value) : null);

        builder.HasOne<OrganizationAdminAccount>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationAdminAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EmployeeAccount>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
