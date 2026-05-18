using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountCreated(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
