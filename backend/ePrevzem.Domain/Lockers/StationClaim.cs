using ePrevzem.Domain.Common;
using ePrevzem.Domain.Lockers.Events;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Lockers;

public sealed class StationClaim : AggregateRoot<StationClaimId>
{
    public PickupStationId PickupStationId { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public DateTimeOffset ClaimedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    public bool IsActive => ReleasedAt is null;

    private StationClaim() { }

    public static StationClaim Claim(
        StationClaimId id,
        PickupStationId stationId,
        OrganizationId organizationId,
        DateTimeOffset now)
    {
        var claim = new StationClaim
        {
            Id = id,
            PickupStationId = stationId,
            OrganizationId = organizationId,
            ClaimedAt = now
        };
        claim.Raise(new StationClaimed(id, stationId, organizationId, now));
        return claim;
    }

    public void Release(DateTimeOffset releasedAt)
    {
        if (ReleasedAt is not null)
            throw new InvalidOperationException("Station claim already released.");
        if (releasedAt < ClaimedAt)
            throw new ArgumentException("Released-at timestamp must be on or after claimed-at.", nameof(releasedAt));

        ReleasedAt = releasedAt;
        Raise(new StationReleased(Id, PickupStationId, OrganizationId, releasedAt));
    }
}
