using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    // ── SystemAdmin variant ──────────────────────────────────────────────────

    [Fact]
    public void IssueForSystemAdmin_creates_token_with_expected_properties()
    {
        var id = RefreshTokenId.New();
        var adminId = SystemAdminId.New();

        var token = RefreshToken.IssueForSystemAdmin(id, adminId, "sha256_hash", Now.AddDays(14), Now);

        token.Id.Should().Be(id);
        token.SystemAdminId.Should().Be(adminId);
        token.OrganizationAdminAccountId.Should().BeNull();
        token.TokenHash.Should().Be("sha256_hash");
        token.ExpiresAt.Should().Be(Now.AddDays(14));
        token.CreatedAt.Should().Be(Now);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenId.Should().BeNull();
        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void IssueForSystemAdmin_Rotate_raises_event_with_system_admin_id()
    {
        var adminId = SystemAdminId.New();
        var token = RefreshToken.IssueForSystemAdmin(RefreshTokenId.New(), adminId, "hash", Now.AddDays(14), Now);
        var newTokenId = RefreshTokenId.New();

        token.Rotate(newTokenId, Now.AddHours(1));

        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.ReplacedByTokenId.Should().Be(newTokenId);
        var ev = token.DomainEvents.OfType<RefreshTokenRotated>().Should().ContainSingle().Subject;
        ev.SystemAdminId.Should().Be(adminId);
        ev.OrganizationAdminAccountId.Should().BeNull();
        ev.NewTokenId.Should().Be(newTokenId);
    }

    // ── OrganizationAdmin variant ────────────────────────────────────────────

    [Fact]
    public void IssueForOrganizationAdmin_creates_token_with_expected_properties()
    {
        var id = RefreshTokenId.New();
        var orgAdminId = OrganizationAdminAccountId.New();

        var token = RefreshToken.IssueForOrganizationAdmin(id, orgAdminId, "sha256_hash", Now.AddDays(14), Now);

        token.Id.Should().Be(id);
        token.OrganizationAdminAccountId.Should().Be(orgAdminId);
        token.SystemAdminId.Should().BeNull();
        token.TokenHash.Should().Be("sha256_hash");
        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void IssueForOrganizationAdmin_Rotate_raises_event_with_org_admin_id()
    {
        var orgAdminId = OrganizationAdminAccountId.New();
        var token = RefreshToken.IssueForOrganizationAdmin(RefreshTokenId.New(), orgAdminId, "hash", Now.AddDays(14), Now);
        var newTokenId = RefreshTokenId.New();

        token.Rotate(newTokenId, Now.AddHours(1));

        var ev = token.DomainEvents.OfType<RefreshTokenRotated>().Should().ContainSingle().Subject;
        ev.OrganizationAdminAccountId.Should().Be(orgAdminId);
        ev.SystemAdminId.Should().BeNull();
    }

    // ── Shared behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void Revoke_sets_RevokedAt()
    {
        var token = SystemAdminToken();
        token.Revoke(Now.AddHours(2));
        token.RevokedAt.Should().Be(Now.AddHours(2));
        token.DomainEvents.OfType<RefreshTokenChainRevoked>().Should().BeEmpty();
    }

    [Fact]
    public void Revoke_twice_throws()
    {
        var token = SystemAdminToken();
        token.Revoke(Now.AddHours(1));
        var act = () => token.Revoke(Now.AddHours(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Revoke_before_CreatedAt_throws()
    {
        var token = SystemAdminToken();
        var act = () => token.Revoke(Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("revokedAt");
    }

    [Fact]
    public void IsActive_returns_true_when_not_revoked_and_not_expired()
    {
        var token = SystemAdminToken();
        token.IsActive(Now.AddDays(7)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_returns_false_when_revoked()
    {
        var token = SystemAdminToken();
        token.Revoke(Now.AddHours(1));
        token.IsActive(Now.AddDays(7)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_returns_false_when_expired()
    {
        var token = SystemAdminToken();
        token.IsActive(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_returns_false_at_exact_expiry_time()
    {
        var token = SystemAdminToken();
        token.IsActive(Now.AddDays(14)).Should().BeFalse();
    }

    [Fact]
    public void Rotate_after_revoke_throws()
    {
        var token = SystemAdminToken();
        token.Revoke(Now.AddHours(1));
        var act = () => token.Rotate(RefreshTokenId.New(), Now.AddHours(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rotate_before_CreatedAt_throws()
    {
        var token = SystemAdminToken();
        var act = () => token.Rotate(RefreshTokenId.New(), Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>();
    }

    private static RefreshToken SystemAdminToken() =>
        RefreshToken.IssueForSystemAdmin(RefreshTokenId.New(), SystemAdminId.New(), "token_hash", Now.AddDays(14), Now);
}
