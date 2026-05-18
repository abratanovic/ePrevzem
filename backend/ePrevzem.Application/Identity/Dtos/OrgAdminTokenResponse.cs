namespace ePrevzem.Application.Identity.Dtos;

public sealed record OrgAdminTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool MustChangePassword);
