using ePrevzem.Domain.Common;

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
        return new PickupStation
        {
            Id = id,
            SerialNumber = serialNumber,
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

    public void SetLockerServiceability(LockerId lockerId, bool isServiceable)
    {
        var locker = _lockers.SingleOrDefault(x => x.Id == lockerId)
            ?? throw new InvalidOperationException($"Locker '{lockerId.Value}' does not belong to this station.");

        if (isServiceable)
            locker.MarkServiceable();
        else
            locker.MarkOutOfService();
    }
}
