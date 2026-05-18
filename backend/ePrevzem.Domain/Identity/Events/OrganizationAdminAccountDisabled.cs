using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record OrganizationAdminAccountDisabled(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
