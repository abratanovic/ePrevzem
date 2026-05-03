using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SiTrustMock;

namespace SiTrustMock.Tests;

public class JwtServiceTests
{
    private const string ValidSecret = "super-secret-key-that-is-long-enough-for-hmac256";

    private static JwtService MakeService(string secret = ValidSecret) =>
        new(new JwtSettings(secret));

    [Fact]
    public void Sign_ReturnsValidJwtString()
    {
        var svc = MakeService();
        var token = svc.Sign(TestData.SampleUser());
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void Sign_TokenContainsAllUserFields()
    {
        var svc = MakeService();
        var user = TestData.SampleUser();
        var tokenStr = svc.Sign(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenStr);

        Assert.Equal(user.FirstName, jwt.Claims.First(c => c.Type == UserData.Claims.FirstName).Value);
        Assert.Equal(user.LastName, jwt.Claims.First(c => c.Type == UserData.Claims.LastName).Value);
        Assert.Equal(user.Emso, jwt.Claims.First(c => c.Type == UserData.Claims.Emso).Value);
        Assert.Equal(user.Phone, jwt.Claims.First(c => c.Type == UserData.Claims.Phone).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == UserData.Claims.Email).Value);
        Assert.Equal(user.DateOfBirth, jwt.Claims.First(c => c.Type == UserData.Claims.DateOfBirth).Value);
        Assert.Equal(user.Address, jwt.Claims.First(c => c.Type == UserData.Claims.Address).Value);
        Assert.Equal(user.Zip, jwt.Claims.First(c => c.Type == UserData.Claims.Zip).Value);
        Assert.Equal(user.City, jwt.Claims.First(c => c.Type == UserData.Claims.City).Value);
    }

    [Fact]
    public void Sign_CorrectSecretVerifiesSuccessfully()
    {
        var svc = MakeService();
        var tokenStr = svc.Sign(TestData.SampleUser());

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidSecret));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = key,
        };

        var principal = handler.ValidateToken(tokenStr, validationParams, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void Sign_WrongSecretFailsVerification()
    {
        var svc = MakeService();
        var tokenStr = svc.Sign(TestData.SampleUser());

        var handler = new JwtSecurityTokenHandler();
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-secret-key-that-is-long-enough-hmac256"));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = wrongKey,
        };

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => handler.ValidateToken(tokenStr, validationParams, out _));
    }
}
