using System.Text.Json;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class ProvisioningCodeConfiguration : IEntityTypeConfiguration<ProvisioningCode>
{
    public void Configure(EntityTypeBuilder<ProvisioningCode> builder)
    {
        builder.ToTable("provisioning_codes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new ProvisioningCodeId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(x => x.Value, x => new OrganizationId(x))
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.OwnsOne(x => x.PreFilledInfo, info =>
        {
            info.Property(x => x.FirstName)
                .HasColumnName("pre_filled_first_name")
                .HasColumnType("text")
                .IsRequired();
            info.Property(x => x.LastName)
                .HasColumnName("pre_filled_last_name")
                .HasColumnType("text")
                .IsRequired();
            info.Property(x => x.Email)
                .HasColumnName("pre_filled_email")
                .HasColumnType("text");
        });

        builder.Property(x => x.Roles)
            .HasColumnName("roles")
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(r => r.ToString()).ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!
                    .Select(Enum.Parse<EmployeeAccountRole>).ToList())
            .IsRequired();

        builder.Property(x => x.CreatedByOrganizationAdminId)
            .HasColumnName("created_by_organization_admin_id")
            .HasConversion(x => x.Value, x => new OrganizationAdminAccountId(x))
            .IsRequired();

        builder.HasOne<OrganizationAdminAccount>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByOrganizationAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RedeemedAt)
            .HasColumnName("redeemed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RedeemedIntoEmployeeAccountId)
            .HasColumnName("redeemed_into_employee_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new EmployeeAccountId(x.Value) : null);

        builder.Property(x => x.IsReprovisioningOfEmployeeAccountId)
            .HasColumnName("is_reprovisioning_of_employee_account_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new EmployeeAccountId(x.Value) : null);

        builder.Ignore(x => x.DomainEvents);
    }
}
