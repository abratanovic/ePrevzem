using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Pickups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Pickups.Persistence;

public sealed class PlacementConfiguration : IEntityTypeConfiguration<Placement>
{
    public void Configure(EntityTypeBuilder<Placement> builder)
    {
        builder.ToTable("placements");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new PlacementId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.PackageId)
            .HasColumnName("package_id")
            .HasConversion(x => x.Value, x => new PackageId(x))
            .IsRequired();

        builder.Property(x => x.LockerId)
            .HasColumnName("locker_id")
            .HasConversion(x => x.Value, x => new LockerId(x))
            .IsRequired();

        builder.Property(x => x.OpenedByEmployeeAccountId)
            .HasColumnName("opened_by_employee_account_id")
            .HasConversion(x => x.Value, x => new EmployeeAccountId(x))
            .IsRequired();

        builder.Property(x => x.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndedAt).HasColumnName("ended_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.EndReason).HasColumnName("end_reason").HasColumnType("text").HasConversion<string>();
        builder.Property(x => x.EndedByCitizenUserId)
            .HasColumnName("ended_by_citizen_user_id")
            .HasConversion(x => x!.Value.Value, x => new CitizenUserId(x));
        builder.Property(x => x.EndedByEmployeeAccountId)
            .HasColumnName("ended_by_employee_account_id")
            .HasConversion(x => x!.Value.Value, x => new EmployeeAccountId(x));

        builder.HasIndex(x => x.PackageId).IsUnique().HasFilter("ended_at IS NULL");
        builder.HasIndex(x => x.LockerId).IsUnique().HasFilter("ended_at IS NULL");
        builder.HasOne<Locker>()
            .WithMany()
            .HasForeignKey(x => x.LockerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeAccount>()
            .WithMany()
            .HasForeignKey(x => x.OpenedByEmployeeAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CitizenUser>()
            .WithMany()
            .HasForeignKey(x => x.EndedByCitizenUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeAccount>()
            .WithMany()
            .HasForeignKey(x => x.EndedByEmployeeAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.IsOpen);
    }
}
