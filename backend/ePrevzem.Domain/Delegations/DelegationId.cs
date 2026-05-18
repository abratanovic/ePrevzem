namespace ePrevzem.Domain.Delegations;

public readonly record struct DelegationId(Guid Value)
{
    public static DelegationId New() => new(Guid.NewGuid());
}
