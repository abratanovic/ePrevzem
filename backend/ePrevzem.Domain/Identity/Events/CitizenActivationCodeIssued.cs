using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenActivationCodeIssued(
    CitizenActivationCodeId Id,
    CitizenUserId CitizenUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
