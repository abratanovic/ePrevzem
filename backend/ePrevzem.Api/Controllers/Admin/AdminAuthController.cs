using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.Refresh;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private const string InvalidCredentialsType = "urn:eprevzem:identity:invalid-credentials";
    private const string InvalidRefreshTokenType = "urn:eprevzem:identity:invalid-refresh-token";

    private readonly IMediator _mediator;

    public AdminAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AdminTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginAdminRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new LoginAdminCommand(request.Username, request.Password),
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
                detail: "Napačno uporabniško ime ali geslo.");
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AdminTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshAdminTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RefreshAdminTokenCommand(request.RefreshToken),
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

public sealed record LoginAdminRequest(string Username, string Password);

public sealed record RefreshAdminTokenRequest(string RefreshToken);
