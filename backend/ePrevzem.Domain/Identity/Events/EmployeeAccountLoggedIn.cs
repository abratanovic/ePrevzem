using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountLoggedIn(
    EmployeeAccountId EmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
