using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record LockerServiceabilityChanged(
    PickupStationId PickupStationId,
    LockerId LockerId,
    bool IsServiceable,
    DateTimeOffset OccurredOn) : IDomainEvent;
