using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.LoginOrgAdmin;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Application.Identity.RefreshOrgAdminToken;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/auth")]
public sealed class OrgAuthController : ControllerBase
{
    private const string InvalidCredentialsType = "urn:eprevzem:identity:invalid-credentials";
    private const string InvalidRefreshTokenType = "urn:eprevzem:identity:invalid-refresh-token";

    private readonly IMediator _mediator;

    public OrgAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [ProducesResponseType<OrgAdminTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginOrgAdminRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new LoginOrganizationAdminCommand(request.Email, request.Password),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (InvalidCredentialsException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                type: InvalidCredentialsType,
                detail: "Napačen e-poštni naslov ali geslo.");
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType<OrgAdminTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshOrgAdminTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RefreshOrganizationAdminTokenCommand(request.RefreshToken),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (InvalidRefreshTokenException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid refresh token",
                type: InvalidRefreshTokenType,
                detail: "Osvežitveni žeton ni veljaven.");
        }
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }
}

public sealed record LoginOrgAdminRequest(string Email, string Password);
public sealed record RefreshOrgAdminTokenRequest(string RefreshToken);
