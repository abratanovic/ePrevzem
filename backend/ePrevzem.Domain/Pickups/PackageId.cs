namespace ePrevzem.Domain.Pickups;

public readonly record struct PackageId(Guid Value)
{
    public static PackageId New() => new(Guid.NewGuid());
}
