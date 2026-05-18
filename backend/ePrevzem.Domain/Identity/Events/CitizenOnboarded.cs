using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenOnboarded(CitizenUserId CitizenUserId, DateTimeOffset OccurredOn) : IDomainEvent;
