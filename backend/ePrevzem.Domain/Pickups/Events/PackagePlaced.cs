using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackagePlaced(
    PackageId PackageId,
    PlacementId PlacementId,
    LockerId LockerId,
    EmployeeAccountId OpenedByEmployeeAccountId,
    DateTimeOffset DeadlineAt,
    DateTimeOffset OccurredOn) : IDomainEvent;
