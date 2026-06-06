using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record LockerCreated(
    PickupStationId PickupStationId,
    LockerId LockerId,
    int LockerNumber,
    DateTimeOffset OccurredOn) : IDomainEvent;
