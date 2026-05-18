using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record RefreshTokenRotated(
    RefreshTokenId OldTokenId,
    RefreshTokenId NewTokenId,
    SystemAdminId? SystemAdminId,
    OrganizationAdminAccountId? OrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
