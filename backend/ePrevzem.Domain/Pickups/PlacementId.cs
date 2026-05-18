namespace ePrevzem.Domain.Pickups;

public readonly record struct PlacementId(Guid Value)
{
    public static PlacementId New() => new(Guid.NewGuid());
}
