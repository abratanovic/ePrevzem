using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Delegations.Events;

public sealed record DelegationRevoked(DelegationId DelegationId, DateTimeOffset OccurredOn) : IDomainEvent;
