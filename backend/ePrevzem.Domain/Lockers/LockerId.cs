namespace ePrevzem.Domain.Lockers;

public readonly record struct LockerId(Guid Value)
{
    public static LockerId New() => new(Guid.NewGuid());
}
