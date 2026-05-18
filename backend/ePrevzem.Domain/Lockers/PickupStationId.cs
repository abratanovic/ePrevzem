namespace ePrevzem.Domain.Lockers;

public readonly record struct PickupStationId(Guid Value)
{
    public static PickupStationId New() => new(Guid.NewGuid());
}
