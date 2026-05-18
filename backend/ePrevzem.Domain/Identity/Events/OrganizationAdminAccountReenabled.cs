using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record OrganizationAdminAccountReenabled(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
