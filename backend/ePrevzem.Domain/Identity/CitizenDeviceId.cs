namespace ePrevzem.Domain.Identity;

public readonly record struct CitizenDeviceId(Guid Value)
{
    public static CitizenDeviceId New() => new(Guid.NewGuid());
}
