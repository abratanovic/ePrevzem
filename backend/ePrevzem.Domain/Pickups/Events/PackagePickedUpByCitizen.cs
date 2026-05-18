using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackagePickedUpByCitizen(
    PackageId PackageId,
    PlacementId PlacementId,
    CitizenUserId PickedUpByCitizenUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
