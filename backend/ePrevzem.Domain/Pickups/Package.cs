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

    public void PickUpByCitizen(CitizenUserId pickedUpBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Pickup is only allowed from InLocker (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByCitizen(pickedUpBy, now);

        Status = PackageStatus.PickedUp;
        FinalizedAt = now;
        Raise(new PackagePickedUpByCitizen(Id, placement.Id, pickedUpBy, now));
    }

    public void RemoveByEmployee(EmployeeAccountId removedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Removal is only allowed from InLocker (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByEmployeeRemoval(removedBy, now);

        Status = PackageStatus.AwaitingPlacement;
        DeadlineAt = null;
        Raise(new PackageRemovedByEmployee(Id, placement.Id, removedBy, now));
    }

    public void MarkExpired(DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Expiry can only be marked from InLocker (current: {Status}).");
        if (DeadlineAt is null || now < DeadlineAt)
            throw new InvalidOperationException("Cannot mark expired before the deadline has passed.");

        Status = PackageStatus.NotPickedUp;
        Raise(new PackageExpired(Id, now));
    }

    public void RetrieveAfterExpiry(EmployeeAccountId retrievedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.NotPickedUp)
            throw new InvalidOperationException($"Retrieval after expiry is only allowed from NotPickedUp (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByExpiryRetrieval(retrievedBy, now);

        Status = PackageStatus.AwaitingPersonalPickup;
        Raise(new PackageRetrievedAfterExpiry(Id, placement.Id, retrievedBy, now));
    }

    public void MarkPickedUpManually(EmployeeAccountId markedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.AwaitingPersonalPickup)
            throw new InvalidOperationException($"Manual mark-picked-up is only allowed from AwaitingPersonalPickup (current: {Status}).");

        Status = PackageStatus.PickedUp;
        FinalizedAt = now;
        Raise(new PackageMarkedPickedUpManually(Id, markedBy, now));
    }
}
