using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record RefreshTokenChainRevoked(
    SystemAdminId? SystemAdminId,
    OrganizationAdminAccountId? OrganizationAdminAccountId,
    EmployeeAccountId? EmployeeAccountId,
    RefreshTokenId TriggerTokenId,
    DateTimeOffset OccurredOn) : IDomainEvent;
