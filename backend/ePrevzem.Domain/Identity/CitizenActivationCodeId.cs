namespace ePrevzem.Domain.Identity;

public readonly record struct CitizenActivationCodeId(Guid Value)
{
    public static CitizenActivationCodeId New() => new(Guid.NewGuid());
}
