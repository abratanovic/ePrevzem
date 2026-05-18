using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageExpired(PackageId PackageId, DateTimeOffset OccurredOn) : IDomainEvent;
