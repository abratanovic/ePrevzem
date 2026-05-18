using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record RefreshTokenChainRevoked(
    SystemAdminId? SystemAdminId,
    OrganizationAdminAccountId? OrganizationAdminAccountId,
    RefreshTokenId TriggerTokenId,
    DateTimeOffset OccurredOn) : IDomainEvent;
