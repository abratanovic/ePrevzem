namespace ePrevzem.Domain.Identity;

public readonly record struct OrganizationAdminAccountId(Guid Value)
{
    public static OrganizationAdminAccountId New() => new(Guid.NewGuid());
}
