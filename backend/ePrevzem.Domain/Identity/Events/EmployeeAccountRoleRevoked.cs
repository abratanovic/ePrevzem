using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountRoleRevoked(
    EmployeeAccountId EmployeeAccountId,
    EmployeeAccountRole Role,
    DateTimeOffset OccurredOn) : IDomainEvent;
