using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    public SystemAdminId? SystemAdminId { get; private set; }
    public OrganizationAdminAccountId? OrganizationAdminAccountId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken IssueForSystemAdmin(
        RefreshTokenId id,
        SystemAdminId systemAdminId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        ValidateCommon(tokenHash, expiresAt, now);
        return new RefreshToken
        {
            Id = id,
            SystemAdminId = systemAdminId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };
    }

    public static RefreshToken IssueForOrganizationAdmin(
        RefreshTokenId id,
        OrganizationAdminAccountId organizationAdminAccountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        ValidateCommon(tokenHash, expiresAt, now);
        return new RefreshToken
        {
            Id = id,
            OrganizationAdminAccountId = organizationAdminAccountId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };
    }

    public void Rotate(RefreshTokenId replacementId, DateTimeOffset now)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Token is already revoked.");
        if (now < CreatedAt)
            throw new ArgumentException("Rotation time must be on or after created-at.", nameof(now));

        RevokedAt = now;
        ReplacedByTokenId = replacementId;
        Raise(new RefreshTokenRotated(Id, replacementId, SystemAdminId, OrganizationAdminAccountId, now));
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Token is already revoked.");
        if (revokedAt < CreatedAt)
            throw new ArgumentException("Revoked-at must be on or after created-at.", nameof(revokedAt));

        RevokedAt = revokedAt;
    }

    public void RecordChainRevocation(DateTimeOffset occurredAt)
    {
        if (occurredAt < CreatedAt)
            throw new ArgumentException("Occurred-at must be on or after created-at.", nameof(occurredAt));

        Raise(new RefreshTokenChainRevoked(SystemAdminId, OrganizationAdminAccountId, Id, occurredAt));
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    private static void ValidateCommon(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration time must be after now.", nameof(expiresAt));
    }
}
