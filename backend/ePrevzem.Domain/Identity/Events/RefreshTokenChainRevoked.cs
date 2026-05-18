using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record RefreshTokenChainRevoked(
    SystemAdminId SystemAdminId,
    RefreshTokenId TriggerTokenId,
    DateTimeOffset OccurredOn) : IDomainEvent;
