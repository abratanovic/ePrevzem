using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.DeviceRefresh;
using ePrevzem.Application.Identity.DeviceVerify;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Refresh;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth/device")]
public sealed class DeviceAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeviceAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("challenge")]
    [ProducesResponseType<DeviceChallengeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Challenge(
        [FromBody] DeviceChallengeRequestBody request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new IssueDeviceChallengeCommand(request.DeviceId),
                cancellationToken);
            return Ok(response);
        }
        catch (DeviceNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Device not found",
                detail: "Naprava ni najdena.");
        }
    }

    [AllowAnonymous]
    [HttpPost("verify")]
    [ProducesResponseType<DeviceSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(
        [FromBody] DeviceVerifyRequestBody request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new VerifyDeviceSignatureCommand(request.DeviceId, request.Signature),
                cancellationToken);
            return Ok(response);
        }
        catch (InvalidChallengeException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid challenge",
                detail: "Avtentikacija ni uspela.");
        }
        catch (InvalidSignatureException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid signature",
                detail: "Avtentikacija ni uspela.");
        }
        catch (DeviceNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Device not found",
                detail: "Naprava ni najdena.");
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<DeviceSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] DeviceRefreshRequestBody request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RefreshDeviceTokenCommand(request.RefreshToken),
                cancellationToken);
            return Ok(response);
        }
        catch (InvalidRefreshTokenException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid refresh token",
                detail: "Osvežitveni žeton ni veljaven.");
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
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

public sealed record DeviceChallengeRequestBody(Guid DeviceId);
public sealed record DeviceVerifyRequestBody(Guid DeviceId, string Signature);
public sealed record DeviceRefreshRequestBody(string RefreshToken);
