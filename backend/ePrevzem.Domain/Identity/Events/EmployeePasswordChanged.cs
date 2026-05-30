using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeePasswordChanged(
    EmployeeAccountId EmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
