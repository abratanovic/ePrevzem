namespace ePrevzem.Domain.Identity;

public readonly record struct CitizenUserId(Guid Value)
{
    public static CitizenUserId New() => new(Guid.NewGuid());
}
