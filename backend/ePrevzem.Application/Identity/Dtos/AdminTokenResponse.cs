namespace ePrevzem.Application.Identity.Dtos;

public sealed record AdminTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
