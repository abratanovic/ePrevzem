using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeDeviceRevoked(
    EmployeeAccountId EmployeeAccountId,
    EmployeeDeviceId EmployeeDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
