namespace ePrevzem.Application.Common.Abstractions;

public interface ISiTrustTokenValidator
{
    SiTrustClaims? Validate(string token);
}

public sealed record SiTrustClaims(
    string FirstName,
    string LastName,
    string Emso,
    string? Email,
    string? Phone);
