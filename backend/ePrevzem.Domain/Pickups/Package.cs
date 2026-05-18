using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups.Events;

namespace ePrevzem.Domain.Pickups;

public sealed class Package : AggregateRoot<PackageId>
{
    private readonly List<Placement> _placements = new();

    public OrganizationId OrganizationId { get; private set; }
    public CitizenUserId RecipientCitizenUserId { get; private set; }
    public EmployeeAccountId CreatedByEmployeeAccountId { get; private set; }
    public PickupStationId TargetPickupStationId { get; private set; }
    public string Description { get; private set; } = default!;
    public PackageStatus Status { get; private set; }
    public DateTimeOffset? DeadlineAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    public IReadOnlyCollection<Placement> Placements => _placements.AsReadOnly();
    public Placement? ActivePlacement => _placements.SingleOrDefault(p => p.IsOpen);

    private Package() { }

    public static Package Create(
        PackageId id,
        OrganizationId organizationId,
        CitizenUserId recipientCitizenUserId,
        EmployeeAccountId createdBy,
        PickupStationId targetPickupStationId,
        string description,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var pkg = new Package
        {
            Id = id,
            OrganizationId = organizationId,
            RecipientCitizenUserId = recipientCitizenUserId,
            CreatedByEmployeeAccountId = createdBy,
            TargetPickupStationId = targetPickupStationId,
            Description = description,
            Status = PackageStatus.AwaitingPlacement,
            CreatedAt = now
        };
        pkg.Raise(new PackageCreated(id, now));
        return pkg;
    }

    public Placement Place(
        PlacementId placementId,
        LockerId lockerId,
        EmployeeAccountId openedBy,
        TimeSpan pickupDuration,
        DateTimeOffset now)
    {
        if (Status != PackageStatus.AwaitingPlacement)
            throw new InvalidOperationException($"Package can only be placed while in AwaitingPlacement (current: {Status}).");
        if (pickupDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pickupDuration), pickupDuration, "Pickup duration must be positive.");

        var placement = Placement.Open(placementId, Id, lockerId, openedBy, now);
        _placements.Add(placement);
        Status = PackageStatus.InLocker;
        DeadlineAt = now + pickupDuration;

        Raise(new PackagePlaced(Id, placementId, lockerId, openedBy, DeadlineAt.Value, now));
        return placement;
    }
}
