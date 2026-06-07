using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ePrevzem.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ePrevzem.Tests.Infrastructure.Identity;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IssueAccessToken_creates_expected_jwt_claims()
    {
        var service = CreateService();
        var admin = global::ePrevzem.Domain.Identity.SystemAdmin.Create(
            global::ePrevzem.Domain.Identity.SystemAdminId.New(),
            "ops-jane",
            "hash",
            Now);

        var result = service.IssueAccessToken(admin);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        result.ExpiresAt.Should().Be(Now.AddMinutes(15));
        token.Issuer.Should().Be("ePrevzem");
        token.Audiences.Should().Contain("ePrevzem.Clients");
        token.Subject.Should().Be(admin.Id.Value.ToString());
        token.Claims.Should().Contain(x => x.Type == ClaimTypes.Role && x.Value == "SystemAdmin");
        token.ValidTo.Should().Be(result.ExpiresAt.UtcDateTime);
    }

    [Fact]
    public void IssueRefreshToken_returns_plaintext_and_sha256_hash_pair()
    {
        var service = CreateService();

        var result = service.IssueRefreshToken(Now);

        result.Plaintext.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().Be(Now.AddDays(14));
        result.Hash.Should().Be(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(result.Plaintext))));
    }

    [Fact]
    public void IssueRefreshToken_returns_different_plaintext_values()
    {
        var service = CreateService();

        var first = service.IssueRefreshToken(Now);
        var second = service.IssueRefreshToken(Now);

        first.Plaintext.Should().NotBe(second.Plaintext);
        first.Hash.Should().NotBe(second.Hash);
    }

    [Fact]
    public void IssueAccessToken_creates_citizen_jwt_with_no_organization_claim()
    {
        var service = CreateService();
        var citizen = global::ePrevzem.Domain.Identity.CitizenUser.Onboard(
            global::ePrevzem.Domain.Identity.CitizenUserId.New(),
            "Marko",
            "Horvat",
            "1234567890123",
            "marko@example.com",
            null,
            Now);

        var result = service.IssueAccessToken(citizen);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        result.ExpiresAt.Should().Be(Now.AddMinutes(15));
        token.Issuer.Should().Be("ePrevzem");
        token.Audiences.Should().Contain("ePrevzem.Clients");
        token.Subject.Should().Be(citizen.Id.Value.ToString());
        token.Claims.Should().Contain(x => x.Type == ClaimTypes.Role && x.Value == "Citizen");
        token.Claims.Should().NotContain(x => x.Type == "organization_id");
        token.ValidTo.Should().Be(result.ExpiresAt.UtcDateTime);
    }

    private static JwtTokenService CreateService()
    {
        var jwtOptions = Options.Create(new JwtTokenOptions
        {
            Issuer = "ePrevzem",
            Audience = "ePrevzem.Clients",
            Secret = "dev-only-secret-replace-me-with-something-long-enough-for-hs256"
        });
        var identityOptions = Options.Create(new IdentityOptions
        {
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 14
        });

        return new JwtTokenService(jwtOptions, identityOptions, new TestClock(Now));
    }
}

internal sealed class TestClock : ePrevzem.Application.Common.Abstractions.IClock
{
    private readonly DateTimeOffset _now;

    public TestClock(DateTimeOffset now)
    {
        _now = now;
    }

    public DateTimeOffset UtcNow => _now;
}
