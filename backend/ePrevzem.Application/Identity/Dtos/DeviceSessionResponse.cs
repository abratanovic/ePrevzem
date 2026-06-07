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
    string? Email,
    string? PhoneNumber,
    string? Emso,
    Guid? OrganizationId,
    string? OrganizationName,
    IReadOnlyList<string> Roles);
