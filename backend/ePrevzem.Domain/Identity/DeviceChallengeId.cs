namespace ePrevzem.Domain.Identity;

public readonly record struct DeviceChallengeId(Guid Value)
{
    public static DeviceChallengeId New() => new(Guid.NewGuid());
}
