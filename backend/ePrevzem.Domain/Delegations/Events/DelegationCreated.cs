using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Domain.Delegations.Events;

public sealed record DelegationCreated(
    DelegationId DelegationId,
    PackageId PackageId,
    CitizenUserId DelegatorCitizenUserId,
    CitizenUserId DelegateCitizenUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
