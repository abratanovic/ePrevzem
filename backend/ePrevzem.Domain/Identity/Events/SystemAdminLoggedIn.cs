using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record SystemAdminLoggedIn(
    SystemAdminId SystemAdminId,
    DateTimeOffset OccurredOn) : IDomainEvent;
