using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace ePrevzem.Infrastructure.Identity;

public sealed class SiTrustTokenValidator : ISiTrustTokenValidator
{
    private readonly TokenValidationParameters _validationParameters;

    public SiTrustTokenValidator(SiTrustOptions options)
    {
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret))
        };
    }

    public SiTrustClaims? Validate(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, _validationParameters, out var validated);
            var jwt = (JwtSecurityToken)validated;

            var firstName = jwt.Claims.FirstOrDefault(c => c.Type == "firstName")?.Value;
            var lastName = jwt.Claims.FirstOrDefault(c => c.Type == "lastName")?.Value;
            var emso = jwt.Claims.FirstOrDefault(c => c.Type == "emso")?.Value;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(emso))
                return null;

            return new SiTrustClaims(
                firstName,
                lastName,
                emso,
                jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                jwt.Claims.FirstOrDefault(c => c.Type == "phone")?.Value);
        }
        catch
        {
            return null;
        }
    }
}
