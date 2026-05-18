using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ITokenService
{
    AccessTokenResult IssueAccessToken(SystemAdmin admin);
    AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin);
    RefreshTokenResult IssueRefreshToken(DateTimeOffset now);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenResult(string Plaintext, string Hash, DateTimeOffset ExpiresAt);
