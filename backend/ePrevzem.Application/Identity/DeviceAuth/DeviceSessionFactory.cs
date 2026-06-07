using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Identity.DeviceAuth;

internal static class DeviceSessionFactory
{
    public static async Task<DeviceSessionResponse> ForCitizenAsync(
        CitizenUser citizen,
        Guid deviceId,
        ITokenService tokens,
        IRefreshTokenRepository refreshRepo,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var access = tokens.IssueAccessToken(citizen);
        var refresh = tokens.IssueRefreshToken(now);
        await refreshRepo.AddAsync(
            RefreshToken.IssueForCitizen(RefreshTokenId.New(), citizen.Id, refresh.Hash, refresh.ExpiresAt, now),
            ct);

        return new DeviceSessionResponse(
            "Citizen",
            deviceId,
            access.Token,
            access.ExpiresAt,
            refresh.Plaintext,
            refresh.ExpiresAt,
            citizen.FirstName,
            citizen.LastName,
            null,
            null,
            Array.Empty<string>());
    }

    public static async Task<DeviceSessionResponse> ForEmployeeAsync(
        EmployeeAccount employee,
        Guid deviceId,
        string? organizationName,
        ITokenService tokens,
        IRefreshTokenRepository refreshRepo,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var access = tokens.IssueAccessToken(employee);
        var refresh = tokens.IssueRefreshToken(now);
        await refreshRepo.AddAsync(
            RefreshToken.IssueForEmployee(RefreshTokenId.New(), employee.Id, refresh.Hash, refresh.ExpiresAt, now),
            ct);

        return new DeviceSessionResponse(
            "Employee",
            deviceId,
            access.Token,
            access.ExpiresAt,
            refresh.Plaintext,
            refresh.ExpiresAt,
            employee.FirstName,
            employee.LastName,
            employee.OrganizationId.Value,
            organizationName,
            employee.Roles.Select(r => r.ToString()).ToList());
    }
}
