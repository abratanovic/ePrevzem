using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers;

public sealed class PickupStation : AggregateRoot<PickupStationId>
{
    private readonly List<Locker> _lockers = new();

    public Location Location { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Locker> Lockers => _lockers.AsReadOnly();

    private PickupStation() { }

    public static PickupStation Create(PickupStationId id, Location location, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(location);
        return new PickupStation
        {
            Id = id,
            Location = location,
            CreatedAt = now
        };
    }

    public Locker AddLocker(LockerId id, int lockerNumber)
    {
        if (_lockers.Any(l => l.LockerNumber == lockerNumber))
            throw new InvalidOperationException($"A locker with locker number {lockerNumber} already exists in this station.");

        var locker = Locker.Create(id, Id, lockerNumber);
        _lockers.Add(locker);
        return locker;
    }
}
