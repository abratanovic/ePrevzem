using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class CitizenActivationCodeConfiguration : IEntityTypeConfiguration<CitizenActivationCode>
{
    public void Configure(EntityTypeBuilder<CitizenActivationCode> builder)
    {
        builder.ToTable("citizen_activation_codes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new CitizenActivationCodeId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.CitizenUserId)
            .HasColumnName("citizen_user_id")
            .HasConversion(x => x.Value, x => new CitizenUserId(x))
            .IsRequired();

        builder.HasOne<CitizenUser>()
            .WithMany()
            .HasForeignKey(x => x.CitizenUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

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

        builder.Ignore(x => x.DomainEvents);
    }
}
