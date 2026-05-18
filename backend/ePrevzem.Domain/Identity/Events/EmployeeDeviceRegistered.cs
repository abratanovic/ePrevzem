using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeDeviceRegistered(
    EmployeeAccountId EmployeeAccountId,
    EmployeeDeviceId EmployeeDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
