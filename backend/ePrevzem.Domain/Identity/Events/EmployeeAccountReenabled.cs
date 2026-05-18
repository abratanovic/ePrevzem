using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountReenabled(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
