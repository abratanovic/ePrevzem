using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record OrganizationAdminPasswordChanged(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
