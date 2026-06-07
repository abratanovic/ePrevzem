using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.IssueProvisioningCode;
using ePrevzem.Application.Identity.PeekProvisioningCode;
using ePrevzem.Application.Identity.RedeemOnboarding;
using ePrevzem.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org")]
public sealed class OrgProvisioningController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgProvisioningController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpPost("provisioning-codes")]
    [ProducesResponseType<IssueProvisioningCodeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Issue(
        [FromBody] IssueProvisioningCodeRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUser.UserId
            ?? throw new InvalidOperationException("User not authenticated.");
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

        try
        {
            var response = await _mediator.Send(
                new IssueProvisioningCodeCommand(
                    accountId,
                    organizationId,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Roles.Select(Enum.Parse<EmployeeAccountRole>).ToList(),
                    request.ExpiresInHours),
                cancellationToken);

            return CreatedAtAction(nameof(Peek), new { code = response.Code }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
    }

    [AllowAnonymous]
    [HttpGet("provisioning/{code}")]
    [ProducesResponseType<PeekProvisioningCodeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Peek([FromRoute] string code, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new PeekProvisioningCodeQuery(code), cancellationToken);
            return Ok(new PeekProvisioningCodeResponse(
                result.FirstName,
                result.LastName,
                result.Email,
                result.Roles,
                result.OrganizationId,
                result.OrganizationName,
                result.ExpiresAt));
        }
        catch (ProvisioningCodeNotFoundException)
        {
            return NotFound();
        }
        catch (ProvisioningCodeExpiredException)
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Code expired",
                detail: "Koda za zagotavljanje je potekla.");
        }
        catch (ProvisioningCodeAlreadyRedeemedException)
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Code already redeemed",
                detail: "Koda za zagotavljanje je bila že unovčena.");
        }
    }

    [AllowAnonymous]
    [HttpPost("provisioning/{code}/redeem")]
    [ProducesResponseType<DeviceSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Redeem(
        [FromRoute] string code,
        [FromBody] RedeemProvisioningRequest request,
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
                detail: "Koda za zagotavljanje je potekla.");
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

public sealed record IssueProvisioningCodeRequest(
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<string> Roles,
    int ExpiresInHours);

public sealed record PeekProvisioningCodeResponse(
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<string> Roles,
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset ExpiresAt);

public sealed record RedeemProvisioningRequest(
    string PublicKeyPem,
    string DeviceFingerprint,
    string? Label);
