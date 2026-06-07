using ePrevzem.Domain.Common;
using ePrevzem.Domain.Lockers.Events;

namespace ePrevzem.Domain.Lockers;

public sealed class PickupStation : AggregateRoot<PickupStationId>
{
    private readonly List<Locker> _lockers = new();

    public string SerialNumber { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Locker> Lockers => _lockers.AsReadOnly();

    private PickupStation() { }

    public static PickupStation Create(PickupStationId id, string serialNumber, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number is required.", nameof(serialNumber));
        var station = new PickupStation
        {
            Id = id,
            SerialNumber = serialNumber,
            CreatedAt = now
        };
        station.Raise(new PickupStationCreated(id, now));
        return station;
    }

    public Locker AddLocker(LockerId id, int lockerNumber, long boxId, DateTimeOffset? now = null)
    {
        if (_lockers.Any(l => l.LockerNumber == lockerNumber))
            throw new InvalidOperationException($"A locker with locker number {lockerNumber} already exists in this station.");

        var locker = Locker.Create(id, Id, lockerNumber, boxId);
        _lockers.Add(locker);
        if (now is not null)
            Raise(new LockerCreated(Id, id, lockerNumber, now.Value));
        return locker;
    }

    public void SetLockerServiceability(LockerId lockerId, bool isServiceable, DateTimeOffset? now = null)
    {
        var locker = _lockers.SingleOrDefault(x => x.Id == lockerId)
            ?? throw new InvalidOperationException($"Locker '{lockerId.Value}' does not belong to this station.");

        if (locker.IsServiceable == isServiceable)
            return;

        if (isServiceable)
            locker.MarkServiceable();
        else
            locker.MarkOutOfService();

        if (now is not null)
            Raise(new LockerServiceabilityChanged(Id, lockerId, isServiceable, now.Value));
    }
}
