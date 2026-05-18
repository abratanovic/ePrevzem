using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageRetrievedAfterExpiry(
    PackageId PackageId,
    PlacementId PlacementId,
    EmployeeAccountId RetrievedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
