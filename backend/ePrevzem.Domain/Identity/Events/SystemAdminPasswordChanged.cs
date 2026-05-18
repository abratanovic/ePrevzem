using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record SystemAdminPasswordChanged(
    SystemAdminId SystemAdminId,
    DateTimeOffset OccurredOn) : IDomainEvent;
