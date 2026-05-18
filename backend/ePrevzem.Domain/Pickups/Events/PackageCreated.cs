using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageCreated(PackageId PackageId, DateTimeOffset OccurredOn) : IDomainEvent;
