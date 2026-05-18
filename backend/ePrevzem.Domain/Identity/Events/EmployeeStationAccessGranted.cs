using ePrevzem.Domain.Common;
using ePrevzem.Domain.Lockers;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeStationAccessGranted(
    EmployeeAccountId EmployeeAccountId,
    PickupStationId PickupStationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
