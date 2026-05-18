using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record ProvisioningCodeIssued(ProvisioningCodeId ProvisioningCodeId, DateTimeOffset OccurredOn) : IDomainEvent;
