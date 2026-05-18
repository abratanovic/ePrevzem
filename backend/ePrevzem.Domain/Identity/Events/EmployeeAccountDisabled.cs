using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountDisabled(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
