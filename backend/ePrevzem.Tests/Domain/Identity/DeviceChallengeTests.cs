using ePrevzem.Domain.Identity;
using Xunit;

namespace ePrevzem.Tests.Domain.Identity;

public class DeviceChallengeTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_sets_fields_and_expiry()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Citizen,
            new byte[] { 1, 2, 3 }, Now, Now.AddMinutes(2));
        Assert.Null(c.ConsumedAt);
        Assert.True(c.IsConsumable(Now));
    }

    [Fact]
    public void Consume_marks_consumed_once()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Citizen,
            new byte[] { 1 }, Now, Now.AddMinutes(2));
        c.Consume(Now);
        Assert.False(c.IsConsumable(Now));
        Assert.Throws<InvalidOperationException>(() => c.Consume(Now));
    }

    [Fact]
    public void Expired_challenge_is_not_consumable()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Employee,
            new byte[] { 1 }, Now, Now.AddMinutes(2));
        Assert.False(c.IsConsumable(Now.AddMinutes(3)));
    }
}
