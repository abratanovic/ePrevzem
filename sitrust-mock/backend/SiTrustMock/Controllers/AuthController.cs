using Microsoft.AspNetCore.Mvc;
using SiTrustMock;

namespace SiTrustMock.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("initiate")]
    public IActionResult Initiate([FromQuery] string? redirectUrl)
    {
        var publicBaseUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
        var baseUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : publicBaseUrl;
        return auth.Initiate(redirectUrl, baseUrl) switch
        {
            InitiateResult.InvalidRequest e => BadRequest(new { error = e.Error }),
            InitiateResult.Success r => Ok(new { r.AttemptId, qrCodeImage = r.QrCodeImage }),
            _ => StatusCode(500)
        };
    }

    [HttpPost("complete")]
    public IActionResult Complete([FromQuery] string? attemptId, [FromQuery] string? emso)
    {
        return auth.Complete(attemptId, emso) switch
        {
            CompleteResult.InvalidRequest e => BadRequest(new { error = e.Error }),
            CompleteResult.UserNotFound => NotFound(new { error = "User not found" }),
            CompleteResult.AttemptNotFound => NotFound(new { error = "Attempt not found" }),
            CompleteResult.Success => Ok(),
            _ => StatusCode(500)
        };
    }

    [HttpGet("check/{attemptId}")]
    public IActionResult Check(string attemptId)
    {
        return auth.Check(attemptId) switch
        {
            AuthCheckResult.NotFound => NotFound(new { error = "Attempt not found" }),
            AuthCheckResult.Pending => Ok(new { status = "pending" }),
            AuthCheckResult.Complete r => Ok(new { status = "complete", redirectUrl = r.RedirectUrl }),
            _ => StatusCode(500)
        };
    }
}
