using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_creates_token_with_expected_properties()
    {
        var id = RefreshTokenId.New();
        var adminId = SystemAdminId.New();
        var tokenHash = "sha256_hash_base64";
        var expiresAt = Now.AddDays(14);

        var token = RefreshToken.Issue(id, adminId, tokenHash, expiresAt, Now);

        token.Id.Should().Be(id);
        token.SystemAdminId.Should().Be(adminId);
        token.TokenHash.Should().Be(tokenHash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.CreatedAt.Should().Be(Now);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenId.Should().BeNull();
        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rotate_sets_RevokedAt_and_ReplacedByTokenId_and_raises_event()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "old_hash",
            Now.AddDays(14),
            Now);

        var newTokenId = RefreshTokenId.New();
        var revokedAt = Now.AddHours(1);

        token.Rotate(newTokenId, revokedAt);

        token.RevokedAt.Should().Be(revokedAt);
        token.ReplacedByTokenId.Should().Be(newTokenId);
        token.DomainEvents.OfType<RefreshTokenRotated>().Should().ContainSingle()
            .Which.Should().Match<RefreshTokenRotated>(e =>
                e.OldTokenId == token.Id &&
                e.NewTokenId == newTokenId &&
                e.SystemAdminId == token.SystemAdminId &&
                e.OccurredOn == revokedAt);
    }

    [Fact]
    public void Revoke_sets_RevokedAt()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        var revokedAt = Now.AddHours(2);
        token.Revoke(revokedAt);

        token.RevokedAt.Should().Be(revokedAt);
        token.DomainEvents.OfType<RefreshTokenChainRevoked>().Should().BeEmpty();
    }

    [Fact]
    public void Revoke_twice_throws()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        token.Revoke(Now.AddHours(1));
        var act = () => token.Revoke(Now.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Revoke_before_CreatedAt_throws()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        var act = () => token.Revoke(Now.AddSeconds(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("revokedAt");
    }

    [Fact]
    public void IsActive_returns_true_when_not_revoked_and_not_expired()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        var isActive = token.IsActive(Now.AddDays(7));

        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_returns_false_when_revoked()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        token.Revoke(Now.AddHours(1));

        var isActive = token.IsActive(Now.AddDays(7));

        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_returns_false_when_expired()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            Now.AddDays(14),
            Now);

        var isActive = token.IsActive(Now.AddDays(15));

        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_returns_false_at_exact_expiry_time()
    {
        var expiresAt = Now.AddDays(14);
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "token_hash",
            expiresAt,
            Now);

        var isActive = token.IsActive(expiresAt);

        isActive.Should().BeFalse();
    }

    [Fact]
    public void Rotate_after_revoke_throws()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "hash",
            Now.AddDays(14),
            Now);

        token.Revoke(Now.AddHours(1));

        var act = () => token.Rotate(RefreshTokenId.New(), Now.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rotate_before_CreatedAt_throws()
    {
        var token = RefreshToken.Issue(
            RefreshTokenId.New(),
            SystemAdminId.New(),
            "hash",
            Now.AddDays(14),
            Now);

        var act = () => token.Rotate(RefreshTokenId.New(), Now.AddSeconds(-1));

        act.Should().Throw<ArgumentException>();
    }
}
