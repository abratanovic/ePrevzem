using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers;

public sealed class Locker : Entity<LockerId>
{
    public PickupStationId PickupStationId { get; private set; }
    public int LockerNumber { get; private set; }
    public bool IsServiceable { get; private set; }

    private Locker() { }

    internal static Locker Create(LockerId id, PickupStationId stationId, int lockerNumber)
    {
        if (lockerNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lockerNumber), lockerNumber, "Locker number must be positive.");

        return new Locker
        {
            Id = id,
            PickupStationId = stationId,
            LockerNumber = lockerNumber,
            IsServiceable = true
        };
    }

    public void MarkOutOfService() => IsServiceable = false;
    public void MarkServiceable() => IsServiceable = true;
}
