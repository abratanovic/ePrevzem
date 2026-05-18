using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Lockers.Persistence;

public sealed class StationClaimConfiguration : IEntityTypeConfiguration<StationClaim>
{
    public void Configure(EntityTypeBuilder<StationClaim> builder)
    {
        builder.ToTable("station_claims");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new StationClaimId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.PickupStationId)
            .HasColumnName("pickup_station_id")
            .HasConversion(x => x.Value, x => new PickupStationId(x))
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(x => x.Value, x => new OrganizationId(x))
            .IsRequired();

        builder.OwnsOne(x => x.Location, loc =>
        {
            loc.Property(x => x.Latitude)
                .HasColumnName("location_latitude")
                .HasColumnType("numeric(9,6)")
                .IsRequired();
            loc.Property(x => x.Longitude)
                .HasColumnName("location_longitude")
                .HasColumnType("numeric(9,6)")
                .IsRequired();
            loc.Property(x => x.Address)
                .HasColumnName("location_address")
                .HasColumnType("text")
                .IsRequired();
            loc.Property(x => x.HouseNumber)
                .HasColumnName("location_house_number")
                .HasColumnType("text")
                .IsRequired();
            loc.Property(x => x.ZipCode)
                .HasColumnName("location_zip_code")
                .HasColumnType("text")
                .IsRequired();
            loc.Property(x => x.City)
                .HasColumnName("location_city")
                .HasColumnType("text")
                .IsRequired();
        });

        builder.Property(x => x.ClaimedAt)
            .HasColumnName("claimed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReleasedAt)
            .HasColumnName("released_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.PickupStationId)
            .IsUnique()
            .HasFilter("released_at IS NULL");

        builder.Ignore(x => x.DomainEvents);
    }
}
