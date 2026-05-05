using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SiTrustMock;

public class JwtService
{
    private readonly string _secret;

    public JwtService(JwtSettings settings)
    {
        _secret = settings.Secret;
    }

    public string Sign(UserData user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(UserData.Claims.FirstName, user.FirstName),
            new Claim(UserData.Claims.LastName, user.LastName),
            new Claim(UserData.Claims.Emso, user.Emso),
            new Claim(UserData.Claims.Phone, user.Phone),
            new Claim(UserData.Claims.Email, user.Email),
            new Claim(UserData.Claims.DateOfBirth, user.DateOfBirth),
            new Claim(UserData.Claims.Address, user.Address),
            new Claim(UserData.Claims.Zip, user.Zip),
            new Claim(UserData.Claims.City, user.City),
        };

        var token = new JwtSecurityToken(claims: claims, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
