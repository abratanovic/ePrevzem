namespace ePrevzem.Domain.Lockers;

public readonly record struct StationClaimId(Guid Value)
{
    public static StationClaimId New() => new(Guid.NewGuid());
}
