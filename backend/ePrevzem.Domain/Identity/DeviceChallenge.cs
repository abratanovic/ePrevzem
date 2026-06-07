using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class DeviceChallenge : AggregateRoot<DeviceChallengeId>
{
    public Guid DeviceId { get; private set; }
    public DeviceKind DeviceKind { get; private set; }
    public byte[] Nonce { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private DeviceChallenge() { }

    public static DeviceChallenge Issue(DeviceChallengeId id, Guid deviceId, DeviceKind kind,
        byte[] nonce, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        if (nonce is null || nonce.Length == 0)
            throw new ArgumentException("Nonce is required.", nameof(nonce));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        return new DeviceChallenge
        {
            Id = id,
            DeviceId = deviceId,
            DeviceKind = kind,
            Nonce = nonce,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
    }

    public bool IsConsumable(DateTimeOffset at) => ConsumedAt is null && at < ExpiresAt;

    public void Consume(DateTimeOffset at)
    {
        if (ConsumedAt is not null)
            throw new InvalidOperationException("Challenge already consumed.");
        if (at >= ExpiresAt)
            throw new InvalidOperationException("Challenge has expired.");

        ConsumedAt = at;
    }
}
