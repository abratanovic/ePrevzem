namespace ePrevzem.Domain.Identity;

public readonly record struct EmployeeAccountId(Guid Value)
{
    public static EmployeeAccountId New() => new(Guid.NewGuid());
}
