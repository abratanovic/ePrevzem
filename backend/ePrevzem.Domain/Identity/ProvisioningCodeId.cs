namespace ePrevzem.Domain.Identity;

public readonly record struct ProvisioningCodeId(Guid Value)
{
    public static ProvisioningCodeId New() => new(Guid.NewGuid());
}
