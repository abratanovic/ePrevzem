using ePrevzem.Domain.Lockers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Lockers.Persistence;

public sealed class LockerConfiguration : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> builder)
    {
        builder.ToTable("lockers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new LockerId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.PickupStationId)
            .HasColumnName("pickup_station_id")
            .HasConversion(x => x.Value, x => new PickupStationId(x))
            .IsRequired();

        builder.Property(x => x.LockerNumber)
            .HasColumnName("locker_number")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.BoxId)
            .HasColumnName("box_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.IsServiceable)
            .HasColumnName("is_serviceable")
            .HasColumnType("boolean")
            .IsRequired();
    }
}
