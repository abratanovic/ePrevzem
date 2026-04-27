using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SiTrustMock;

public class JwtService
{
    private readonly string _secret;

    public JwtService(IConfiguration config)
    {
        _secret = config["JwtSettings:Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret is not configured");
    }

    public string Sign(UserData user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("emso", user.Emso),
            new Claim("phone", user.Phone),
            new Claim("email", user.Email),
            new Claim("dateOfBirth", user.DateOfBirth),
            new Claim("address", user.Address),
            new Claim("zip", user.Zip),
            new Claim("city", user.City),
        };

        var token = new JwtSecurityToken(claims: claims, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
