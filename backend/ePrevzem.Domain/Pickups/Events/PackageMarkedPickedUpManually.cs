using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageMarkedPickedUpManually(
    PackageId PackageId,
    EmployeeAccountId MarkedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
