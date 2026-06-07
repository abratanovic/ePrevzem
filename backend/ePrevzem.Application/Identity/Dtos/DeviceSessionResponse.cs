namespace ePrevzem.Application.Identity.Dtos;

public sealed record DeviceSessionResponse(
    string Role,
    Guid DeviceId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string FirstName,
    string LastName,
    Guid? OrganizationId,
    string? OrganizationName,
    IReadOnlyList<string> Roles);
