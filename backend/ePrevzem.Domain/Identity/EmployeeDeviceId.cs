namespace ePrevzem.Domain.Identity;

public readonly record struct EmployeeDeviceId(Guid Value)
{
    public static EmployeeDeviceId New() => new(Guid.NewGuid());
}
