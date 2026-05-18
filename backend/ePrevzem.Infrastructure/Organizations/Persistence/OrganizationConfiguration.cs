using ePrevzem.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Organizations.Persistence;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new OrganizationId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.TaxNumber)
            .HasColumnName("tax_number")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.TaxNumber)
            .IsUnique();

        builder.Property(x => x.RegistrationNumber)
            .HasColumnName("registration_number")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.RegistrationNumber)
            .IsUnique();

        builder.Property(x => x.DefaultPickupDuration)
            .HasColumnName("default_pickup_duration_in_days")
            .HasColumnType("integer")
            .HasConversion(
                ts => (int)ts.TotalDays,
                days => TimeSpan.FromDays(days))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
