using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record StationReleased(
    StationClaimId StationClaimId,
    PickupStationId PickupStationId,
    OrganizationId OrganizationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
