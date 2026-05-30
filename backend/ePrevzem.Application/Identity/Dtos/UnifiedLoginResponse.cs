namespace ePrevzem.Application.Identity.Dtos;

public sealed record UnifiedLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string Role,
    bool MustChangePassword,
    string FirstName,
    string LastName,
    string? Email,
    Guid? OrganizationId,
    string? OrganizationName);
