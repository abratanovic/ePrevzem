using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ePrevzem.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenOptions _jwtOptions;
    private readonly IdentityOptions _identityOptions;
    private readonly IClock _clock;

    public JwtTokenService(
        IOptions<JwtTokenOptions> jwtOptions,
        IOptions<IdentityOptions> identityOptions,
        IClock clock)
    {
        _jwtOptions = jwtOptions.Value;
        _identityOptions = identityOptions.Value;
        _clock = clock;
    }

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_identityOptions.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.Value.ToString()),
            new Claim(ClaimTypes.Role, "OrganizationAdmin"),
            new Claim("organization_id", admin.OrganizationId.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_identityOptions.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.Value.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin"),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_identityOptions.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, employee.Id.Value.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("organization_id", employee.OrganizationId.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
    {
        var plaintextBytes = RandomNumberGenerator.GetBytes(32);
        var plaintext = Base64UrlEncoder.Encode(plaintextBytes);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        var expiresAt = now.AddDays(_identityOptions.RefreshTokenLifetimeDays);

        return new RefreshTokenResult(
            plaintext,
            Convert.ToBase64String(hash),
            expiresAt);
    }
}
