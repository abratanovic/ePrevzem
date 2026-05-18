using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;

namespace ePrevzem.Domain.Pickups;

public sealed class Placement : Entity<PlacementId>
{
    public PackageId PackageId { get; private set; }
    public LockerId LockerId { get; private set; }
    public EmployeeAccountId OpenedByEmployeeAccountId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public PlacementEndReason? EndReason { get; private set; }
    public CitizenUserId? EndedByCitizenUserId { get; private set; }
    public EmployeeAccountId? EndedByEmployeeAccountId { get; private set; }

    public bool IsOpen => EndedAt is null;

    private Placement() { }

    internal static Placement Open(
        PlacementId id,
        PackageId packageId,
        LockerId lockerId,
        EmployeeAccountId openedBy,
        DateTimeOffset openedAt)
    {
        return new Placement
        {
            Id = id,
            PackageId = packageId,
            LockerId = lockerId,
            OpenedByEmployeeAccountId = openedBy,
            OpenedAt = openedAt
        };
    }

    internal void CloseByCitizen(CitizenUserId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.PickedUpByCitizen;
        EndedByCitizenUserId = endedBy;
    }

    internal void CloseByEmployeeRemoval(EmployeeAccountId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.RemovedByEmployee;
        EndedByEmployeeAccountId = endedBy;
    }

    internal void CloseByExpiryRetrieval(EmployeeAccountId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.RetrievedAfterExpiry;
        EndedByEmployeeAccountId = endedBy;
    }

    private void EnsureOpen(DateTimeOffset endedAt)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Placement is already closed.");
        if (endedAt < OpenedAt)
            throw new ArgumentException("Ended-at must be on or after opened-at.", nameof(endedAt));
    }
}
