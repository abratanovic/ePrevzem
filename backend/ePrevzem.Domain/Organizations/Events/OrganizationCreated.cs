using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Organizations.Events;

public sealed record OrganizationCreated(OrganizationId OrganizationId, DateTimeOffset OccurredOn) : IDomainEvent;
