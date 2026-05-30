using System.Text.Json;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class EmployeeAccountConfiguration : IEntityTypeConfiguration<EmployeeAccount>
{
    public void Configure(EntityTypeBuilder<EmployeeAccount> builder)
    {
        builder.ToTable("employee_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new EmployeeAccountId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(x => x.Value, x => new OrganizationId(x))
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("text");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text");

        builder.Property(x => x.MustChangePassword)
            .HasColumnName("must_change_password")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.CreatedFromProvisioningCodeId)
            .HasColumnName("created_from_provisioning_code_id")
            .HasConversion(x => x.Value, x => new ProvisioningCodeId(x))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Roles stored as JSON via private backing field
        builder.Property<List<EmployeeAccountRole>>("_roles")
            .HasColumnName("roles")
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(r => r.ToString()).ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!
                    .Select(Enum.Parse<EmployeeAccountRole>).ToList())
            .IsRequired();

        // Station access stored as JSON via private backing field
        builder.Property<List<PickupStationId>>("_stationAccess")
            .HasColumnName("station_access")
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(id => id.Value).ToList(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!
                    .Select(g => new PickupStationId(g)).ToList())
            .IsRequired();

        builder.OwnsMany(x => x.Devices, devices =>
        {
            devices.ToTable("employee_devices");
            devices.WithOwner().HasForeignKey("employee_account_id");
            devices.HasKey(x => x.Id);
            devices.Property(x => x.Id)
                .HasConversion(x => x.Value, x => new EmployeeDeviceId(x))
                .ValueGeneratedNever();
            devices.Property(x => x.EmployeeAccountId)
                .HasColumnName("employee_account_id")
                .HasConversion(x => x.Value, x => new EmployeeAccountId(x))
                .IsRequired();
            devices.Property(x => x.PublicKey)
                .HasColumnName("public_key")
                .IsRequired();
            devices.Property(x => x.DeviceFingerprint)
                .HasColumnName("device_fingerprint")
                .HasColumnType("text")
                .IsRequired();
            devices.Property(x => x.Label)
                .HasColumnName("label")
                .HasColumnType("text");
            devices.Property(x => x.ProvisionedAt)
                .HasColumnName("provisioned_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            devices.Property(x => x.RevokedAt)
                .HasColumnName("revoked_at")
                .HasColumnType("timestamp with time zone");
            devices.Ignore(x => x.IsActive);
        });

        builder.Ignore(x => x.DomainEvents);
    }
}
