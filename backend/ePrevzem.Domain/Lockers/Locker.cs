using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers;

public sealed class Locker : Entity<LockerId>
{
    public PickupStationId PickupStationId { get; private set; }
    public int LockerNumber { get; private set; }

    /// <summary>
    /// The Direct4Me hardware box identifier used to open this physical locker.
    /// Resolved server-side when opening; never exposed to or supplied by clients.
    /// </summary>
    public long BoxId { get; private set; }
    public bool IsServiceable { get; private set; }

    private Locker() { }

    internal static Locker Create(LockerId id, PickupStationId stationId, int lockerNumber, long boxId)
    {
        if (lockerNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lockerNumber), lockerNumber, "Locker number must be positive.");
        if (boxId <= 0)
            throw new ArgumentOutOfRangeException(nameof(boxId), boxId, "Box id must be positive.");

        return new Locker
        {
            Id = id,
            PickupStationId = stationId,
            LockerNumber = lockerNumber,
            BoxId = boxId,
            IsServiceable = true
        };
    }

    public void MarkOutOfService() => IsServiceable = false;
    public void MarkServiceable() => IsServiceable = true;
}
