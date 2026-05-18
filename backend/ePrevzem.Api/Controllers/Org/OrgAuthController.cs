using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.ChangeOrgAdminPassword;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.LoginOrgAdmin;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Application.Identity.RefreshOrgAdminToken;
using Microsoft.AspNetCore.Authorization;
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
    private const string WrongPasswordType = "urn:eprevzem:identity:wrong-current-password";

    private readonly ICurrentUser _currentUser;

    private readonly IMediator _mediator;

    public OrgAuthController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeOrgAdminPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUser.UserId
            ?? throw new InvalidOperationException("User not authenticated.");

        try
        {
            await _mediator.Send(
                new ChangeOrganizationAdminPasswordCommand(accountId, request.CurrentPassword, request.NewPassword),
                cancellationToken);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (WrongCurrentPasswordException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Wrong current password",
                type: WrongPasswordType,
                detail: "Trenutno geslo ni pravilno.");
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
public sealed record ChangeOrgAdminPasswordRequest(string CurrentPassword, string NewPassword);
