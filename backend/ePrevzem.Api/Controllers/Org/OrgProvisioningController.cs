using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.IssueProvisioningCode;
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
                    request.StationAccessIds,
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
    public Task<IActionResult> Peek([FromRoute] string code, CancellationToken cancellationToken)
    {
        // TODO: implement PeekProvisioningCodeQuery handler
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [AllowAnonymous]
    [HttpPost("provisioning/{code}/redeem")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> Redeem([FromRoute] string code, CancellationToken cancellationToken)
    {
        // TODO: implement device provisioning — create EmployeeAccount, register device, return tokens
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
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
    IReadOnlyList<Guid> StationAccessIds,
    int ExpiresInHours);

public sealed record PeekProvisioningCodeResponse(
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<string> Roles,
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset ExpiresAt);
