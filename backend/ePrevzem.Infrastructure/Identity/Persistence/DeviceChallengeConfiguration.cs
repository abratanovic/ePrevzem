using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class DeviceChallengeConfiguration : IEntityTypeConfiguration<DeviceChallenge>
{
    public void Configure(EntityTypeBuilder<DeviceChallenge> builder)
    {
        builder.ToTable("device_challenges");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new DeviceChallengeId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        builder.Property(x => x.DeviceKind)
            .HasColumnName("device_kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Nonce)
            .HasColumnName("nonce")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.DeviceId, x.ConsumedAt });

        builder.Ignore(x => x.DomainEvents);
    }
}
