using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Lockers.Events;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class StationClaimTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Claim_creates_active_claim_and_raises_event()
    {
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            PickupStationId.New(),
            OrganizationId.New(),
            Now);

        claim.ClaimedAt.Should().Be(Now);
        claim.ReleasedAt.Should().BeNull();
        claim.IsActive.Should().BeTrue();
        claim.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StationClaimed>();
    }

    [Fact]
    public void Release_sets_ReleasedAt_and_raises_event()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        var later = Now.AddDays(1);

        claim.Release(later);

        claim.ReleasedAt.Should().Be(later);
        claim.IsActive.Should().BeFalse();
        claim.DomainEvents.OfType<StationReleased>().Should().ContainSingle();
    }

    [Fact]
    public void Release_when_already_released_throws()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        claim.Release(Now.AddDays(1));

        var act = () => claim.Release(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already released*");
    }

    [Fact]
    public void Release_before_ClaimedAt_throws()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        var act = () => claim.Release(Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("releasedAt");
    }
}
