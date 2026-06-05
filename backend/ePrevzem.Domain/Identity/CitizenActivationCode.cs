using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class CitizenActivationCode : AggregateRoot<CitizenActivationCodeId>
{
    public CitizenUserId CitizenUserId { get; private set; }
    public string Code { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }

    private CitizenActivationCode() { }

    public static CitizenActivationCode Issue(
        CitizenActivationCodeId id,
        CitizenUserId citizenUserId,
        string code,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        var ac = new CitizenActivationCode
        {
            Id = id,
            CitizenUserId = citizenUserId,
            Code = code,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        ac.Raise(new CitizenActivationCodeIssued(id, citizenUserId, now));
        return ac;
    }

    public bool IsRedeemable(DateTimeOffset at) => RedeemedAt is null && at < ExpiresAt;

    public void Redeem(DateTimeOffset at)
    {
        if (RedeemedAt is not null)
            throw new InvalidOperationException("Activation code is already redeemed.");
        if (at >= ExpiresAt)
            throw new InvalidOperationException("Activation code has expired.");

        RedeemedAt = at;
    }
}
