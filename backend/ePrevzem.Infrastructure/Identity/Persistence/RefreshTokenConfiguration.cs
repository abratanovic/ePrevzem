using ePrevzem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ePrevzem.Infrastructure.Identity.Persistence;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new RefreshTokenId(x))
            .ValueGeneratedNever();

        builder.Property(x => x.SystemAdminId)
            .HasColumnName("system_admin_id")
            .HasConversion(x => x.Value, x => new SystemAdminId(x))
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? new RefreshTokenId(x.Value) : null);

        builder.HasIndex(x => new { x.SystemAdminId, x.RevokedAt });

        builder.HasOne<SystemAdmin>()
            .WithMany()
            .HasForeignKey(x => x.SystemAdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
