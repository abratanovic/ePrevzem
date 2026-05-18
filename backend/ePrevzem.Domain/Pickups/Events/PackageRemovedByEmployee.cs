using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageRemovedByEmployee(
    PackageId PackageId,
    PlacementId PlacementId,
    EmployeeAccountId RemovedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
