using ePrevzem.Domain.Common;
using ePrevzem.Domain.Delegations.Events;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Domain.Delegations;

public sealed class Delegation : AggregateRoot<DelegationId>
{
    public PackageId PackageId { get; private set; }
    public CitizenUserId DelegatorCitizenUserId { get; private set; }
    public CitizenUserId DelegateCitizenUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    private Delegation() { }

    public static Delegation Create(
        DelegationId id,
        PackageId packageId,
        CitizenUserId delegator,
        CitizenUserId @delegate,
        DateTimeOffset now)
    {
        if (delegator == @delegate)
            throw new ArgumentException("A citizen cannot delegate to themselves.", nameof(@delegate));

        var d = new Delegation
        {
            Id = id,
            PackageId = packageId,
            DelegatorCitizenUserId = delegator,
            DelegateCitizenUserId = @delegate,
            CreatedAt = now
        };
        d.Raise(new DelegationCreated(id, packageId, delegator, @delegate, now));
        return d;
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Delegation is already revoked.");
        if (revokedAt < CreatedAt)
            throw new ArgumentException("Revoked-at must be on or after created-at.", nameof(revokedAt));

        RevokedAt = revokedAt;
        Raise(new DelegationRevoked(Id, revokedAt));
    }
}
