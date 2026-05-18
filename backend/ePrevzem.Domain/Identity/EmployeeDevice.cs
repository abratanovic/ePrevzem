using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class EmployeeDevice : Entity<EmployeeDeviceId>
{
    public EmployeeAccountId EmployeeAccountId { get; private set; }
    public byte[] PublicKey { get; private set; } = default!;
    public string DeviceFingerprint { get; private set; } = default!;
    public string? Label { get; private set; }
    public DateTimeOffset ProvisionedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private EmployeeDevice() { }

    internal static EmployeeDevice Register(
        EmployeeDeviceId id,
        EmployeeAccountId accountId,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset provisionedAt)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("Public key is required.", nameof(publicKey));
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
            throw new ArgumentException("Device fingerprint is required.", nameof(deviceFingerprint));

        return new EmployeeDevice
        {
            Id = id,
            EmployeeAccountId = accountId,
            PublicKey = publicKey,
            DeviceFingerprint = deviceFingerprint,
            Label = label,
            ProvisionedAt = provisionedAt
        };
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Device is already revoked.");
        RevokedAt = revokedAt;
    }
}
