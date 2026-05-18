using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class CitizenDevice : Entity<CitizenDeviceId>
{
    public CitizenUserId CitizenUserId { get; private set; }
    public byte[] PublicKey { get; private set; } = default!;
    public string DeviceFingerprint { get; private set; } = default!;
    public string? Label { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private CitizenDevice() { }

    internal static CitizenDevice Register(
        CitizenDeviceId id,
        CitizenUserId citizenUserId,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset registeredAt)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("Public key is required.", nameof(publicKey));
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
            throw new ArgumentException("Device fingerprint is required.", nameof(deviceFingerprint));

        return new CitizenDevice
        {
            Id = id,
            CitizenUserId = citizenUserId,
            PublicKey = publicKey,
            DeviceFingerprint = deviceFingerprint,
            Label = label,
            RegisteredAt = registeredAt
        };
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Device is already revoked.");
        RevokedAt = revokedAt;
    }
}
