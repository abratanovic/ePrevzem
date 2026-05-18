using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity.Events;

public sealed record OrganizationAdminAccountCreated(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    OrganizationId OrganizationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
