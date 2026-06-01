using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Pickups.Persistence;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages", table =>
            table.HasCheckConstraint(
                "ck_packages_exactly_one_creator",
                "(created_by_employee_account_id IS NOT NULL) <> (created_by_organization_admin_account_id IS NOT NULL)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new PackageId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(x => x.Value, x => new OrganizationId(x))
            .IsRequired();

        builder.Property(x => x.RecipientCitizenUserId)
            .HasColumnName("recipient_citizen_user_id")
            .HasConversion(x => x.Value, x => new CitizenUserId(x))
            .IsRequired();

        builder.Property(x => x.CreatedByEmployeeAccountId)
            .HasColumnName("created_by_employee_account_id")
            .HasConversion(x => x!.Value.Value, x => new EmployeeAccountId(x));

        builder.Property(x => x.CreatedByOrganizationAdminAccountId)
            .HasColumnName("created_by_organization_admin_account_id")
            .HasConversion(x => x!.Value.Value, x => new OrganizationAdminAccountId(x));

        builder.Property(x => x.TargetPickupStationId)
            .HasColumnName("target_pickup_station_id")
            .HasConversion(x => x.Value, x => new PickupStationId(x))
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasColumnName("reference")
            .HasColumnType("text")
            .IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.DeadlineAt)
            .HasColumnName("deadline_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.FinalizedAt)
            .HasColumnName("finalized_at")
            .HasColumnType("timestamp with time zone");

        builder.HasMany(x => x.Placements)
            .WithOne()
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CitizenUser>()
            .WithMany()
            .HasForeignKey(x => x.RecipientCitizenUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeAccount>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByEmployeeAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrganizationAdminAccount>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByOrganizationAdminAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PickupStation>()
            .WithMany()
            .HasForeignKey(x => x.TargetPickupStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.ActivePlacement);
        builder.Ignore(x => x.DomainEvents);
    }
}
