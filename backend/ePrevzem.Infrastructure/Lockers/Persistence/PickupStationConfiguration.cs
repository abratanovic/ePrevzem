using ePrevzem.Domain.Lockers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Lockers.Persistence;

public sealed class PickupStationConfiguration : IEntityTypeConfiguration<PickupStation>
{
    public void Configure(EntityTypeBuilder<PickupStation> builder)
    {
        builder.ToTable("pickup_stations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new PickupStationId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.SerialNumber)
            .HasColumnName("serial_number")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.SerialNumber)
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasMany(x => x.Lockers)
            .WithOne()
            .HasForeignKey(x => x.PickupStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
