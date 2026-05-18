using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenDeviceRevoked(
    CitizenUserId CitizenUserId,
    CitizenDeviceId CitizenDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
