namespace ePrevzem.Domain.Identity;

public readonly record struct SystemAdminId(Guid Value)
{
    public static SystemAdminId New() => new(Guid.NewGuid());
}
