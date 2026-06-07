using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.PeekOnboarding;
using ePrevzem.Application.Identity.RedeemOnboarding;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Onboarding;

[ApiController]
[Route("api/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    private readonly IMediator _mediator;

    public OnboardingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("{code}")]
    [ProducesResponseType<OnboardingPreview>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Peek([FromRoute] string code, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new PeekOnboardingCodeQuery(code), cancellationToken);
            return Ok(result);
        }
        catch (OnboardingCodeNotFoundException)
        {
            return NotFound();
        }
        catch (OnboardingCodeExpiredException)
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Code expired",
                detail: "Koda je potekla ali je bila že uporabljena.");
        }
    }

    [AllowAnonymous]
    [HttpPost("{code}/redeem")]
    [ProducesResponseType<DeviceSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Redeem(
        [FromRoute] string code,
        [FromBody] RedeemOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RedeemOnboardingCodeCommand(code, request.PublicKeyPem, request.DeviceFingerprint, request.Label),
                cancellationToken);
            return Ok(response);
        }
        catch (OnboardingCodeNotFoundException)
        {
            return NotFound();
        }
        catch (OnboardingCodeExpiredException)
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Code expired",
                detail: "Koda je potekla ali je bila že uporabljena.");
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

public sealed record RedeemOnboardingRequest(
    string PublicKeyPem,
    string DeviceFingerprint,
    string? Label);
