using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    public SystemAdminId SystemAdminId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(
        RefreshTokenId id,
        SystemAdminId systemAdminId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration time must be after now.", nameof(expiresAt));

        var token = new RefreshToken
        {
            Id = id,
            SystemAdminId = systemAdminId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

        return token;
    }

    public void Rotate(RefreshTokenId replacementId, DateTimeOffset now)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Token is already revoked.");
        if (now < CreatedAt)
            throw new ArgumentException("Rotation time must be on or after created-at.", nameof(now));

        RevokedAt = now;
        ReplacedByTokenId = replacementId;
        Raise(new RefreshTokenRotated(Id, replacementId, SystemAdminId, now));
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Token is already revoked.");
        if (revokedAt < CreatedAt)
            throw new ArgumentException("Revoked-at must be on or after created-at.", nameof(revokedAt));

        RevokedAt = revokedAt;
        Raise(new RefreshTokenChainRevoked(SystemAdminId, Id, revokedAt));
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
