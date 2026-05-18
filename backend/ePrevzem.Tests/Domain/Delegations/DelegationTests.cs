using ePrevzem.Domain.Delegations;
using ePrevzem.Domain.Delegations.Events;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Delegations;

public class DelegationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_active_delegation_and_raises_event()
    {
        var id = DelegationId.New();
        var package = PackageId.New();
        var delegator = CitizenUserId.New();
        var delegate_ = CitizenUserId.New();

        var d = Delegation.Create(id, package, delegator, delegate_, Now);

        d.Id.Should().Be(id);
        d.PackageId.Should().Be(package);
        d.DelegatorCitizenUserId.Should().Be(delegator);
        d.DelegateCitizenUserId.Should().Be(delegate_);
        d.CreatedAt.Should().Be(Now);
        d.RevokedAt.Should().BeNull();
        d.IsRevoked.Should().BeFalse();
        d.DomainEvents.OfType<DelegationCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_with_self_delegation_throws()
    {
        var user = CitizenUserId.New();
        var act = () => Delegation.Create(DelegationId.New(), PackageId.New(), user, user, Now);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot delegate to themselves*");
    }

    [Fact]
    public void Revoke_sets_RevokedAt_and_raises_event()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        d.Revoke(Now.AddDays(1));

        d.RevokedAt.Should().Be(Now.AddDays(1));
        d.IsRevoked.Should().BeTrue();
        d.DomainEvents.OfType<DelegationRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void Revoke_twice_throws()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        d.Revoke(Now.AddDays(1));
        var act = () => d.Revoke(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Revoke_before_CreatedAt_throws()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        var act = () => d.Revoke(Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("revokedAt");
    }
}
