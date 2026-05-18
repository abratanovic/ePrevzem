using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record OrganizationAdminLoggedIn(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
