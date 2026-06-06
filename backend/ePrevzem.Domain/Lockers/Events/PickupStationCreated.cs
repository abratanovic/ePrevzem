using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record PickupStationCreated(
    PickupStationId PickupStationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
