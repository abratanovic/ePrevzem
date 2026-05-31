using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class CitizenUserConfiguration : IEntityTypeConfiguration<CitizenUser>
{
    public void Configure(EntityTypeBuilder<CitizenUser> builder)
    {
        builder.ToTable("citizen_users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new CitizenUserId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Emso)
            .HasColumnName("emso")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.Emso).IsUnique();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("text");

        builder.Property(x => x.PhoneNumber)
            .HasColumnName("phone_number")
            .HasColumnType("text");

        builder.Property(x => x.OnboardedAt)
            .HasColumnName("onboarded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.OwnsMany(x => x.Devices, devices =>
        {
            devices.ToTable("citizen_devices");
            devices.WithOwner().HasForeignKey("citizen_user_id");
            devices.HasKey(x => x.Id);
            devices.Property(x => x.Id)
                .HasConversion(x => x.Value, x => new CitizenDeviceId(x))
                .ValueGeneratedNever();
            devices.Property(x => x.CitizenUserId)
                .HasColumnName("citizen_user_id")
                .HasConversion(x => x.Value, x => new CitizenUserId(x))
                .IsRequired();
            devices.Property(x => x.PublicKey)
                .HasColumnName("public_key")
                .HasColumnType("bytea")
                .IsRequired();
            devices.Property(x => x.DeviceFingerprint)
                .HasColumnName("device_fingerprint")
                .HasColumnType("text")
                .IsRequired();
            devices.Property(x => x.Label)
                .HasColumnName("label")
                .HasColumnType("text");
            devices.Property(x => x.RegisteredAt)
                .HasColumnName("registered_at")
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
