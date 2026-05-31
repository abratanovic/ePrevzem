using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations;
using ePrevzem.Application.Organizations.AddMember;
using ePrevzem.Application.Organizations.DisableEmployee;
using ePrevzem.Application.Organizations.GrantEmployeeRole;
using ePrevzem.Application.Organizations.ListMembers;
using ePrevzem.Application.Organizations.ReenableEmployee;
using ePrevzem.Application.Organizations.ReissueProvisioningCode;
using ePrevzem.Application.Organizations.RevokeEmployeeRole;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/members")]
[Authorize]
public sealed class OrgMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgMembersController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private const string DuplicateEmailType = "urn:eprevzem:members:duplicate-email";
    private const string EmployeeNotFoundType = "urn:eprevzem:members:not-found";
    private const string EmployeeForbiddenType = "urn:eprevzem:members:forbidden";
    private const string EmployeeAlreadyInStateType = "urn:eprevzem:members:already-in-state";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MemberDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

        var members = await _mediator.Send(new ListOrganisationMembersQuery(organizationId), cancellationToken);
        return Ok(members);
    }

    [HttpPost]
    [ProducesResponseType<AddEmployeeMemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        [FromBody] AddEmployeeMemberRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUser.UserId
            ?? throw new InvalidOperationException("User not authenticated.");
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

        try
        {
            var response = await _mediator.Send(
                new AddEmployeeMemberCommand(accountId, organizationId, request.FirstName, request.LastName, request.Email),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (DuplicateEmployeeEmailException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate email",
                type: DuplicateEmailType,
                detail: "Zaposleni s tem e-poštnim naslovom že obstaja.");
        }
    }

    [HttpPatch("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Disable([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");
        try
        {
            await _mediator.Send(new DisableEmployeeCommand(organizationId, id), cancellationToken);
            return NoContent();
        }
        catch (EmployeeNotFoundException)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, type: EmployeeNotFoundType, title: "Employee not found");
        }
        catch (EmployeeForbiddenException)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: EmployeeForbiddenType, title: "Forbidden");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already disabled"))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, type: EmployeeAlreadyInStateType, title: "Already disabled");
        }
    }

    [HttpPatch("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enable([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");
        try
        {
            await _mediator.Send(new ReenableEmployeeCommand(organizationId, id), cancellationToken);
            return NoContent();
        }
        catch (EmployeeNotFoundException)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, type: EmployeeNotFoundType, title: "Employee not found");
        }
        catch (EmployeeForbiddenException)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: EmployeeForbiddenType, title: "Forbidden");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already active"))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, type: EmployeeAlreadyInStateType, title: "Already active");
        }
    }

    [HttpPatch("{id:guid}/roles/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeRole([FromRoute] Guid id, [FromBody] RoleRequest request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");
        try
        {
            await _mediator.Send(new RevokeEmployeeRoleCommand(organizationId, id, request.Role), cancellationToken);
            return NoContent();
        }
        catch (EmployeeNotFoundException)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, type: EmployeeNotFoundType, title: "Employee not found");
        }
        catch (EmployeeForbiddenException)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: EmployeeForbiddenType, title: "Forbidden");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, type: EmployeeAlreadyInStateType, title: "Role conflict", detail: ex.Message);
        }
    }

    [HttpPatch("{id:guid}/roles/grant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantRole([FromRoute] Guid id, [FromBody] RoleRequest request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");
        try
        {
            await _mediator.Send(new GrantEmployeeRoleCommand(organizationId, id, request.Role), cancellationToken);
            return NoContent();
        }
        catch (EmployeeNotFoundException)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, type: EmployeeNotFoundType, title: "Employee not found");
        }
        catch (EmployeeForbiddenException)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: EmployeeForbiddenType, title: "Forbidden");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, type: EmployeeAlreadyInStateType, title: "Role conflict", detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/provisioning-code")]
    [ProducesResponseType<ReissueProvisioningCodeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReissueProvisioningCode([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.UserId
            ?? throw new InvalidOperationException("User not authenticated.");
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");
        try
        {
            var response = await _mediator.Send(
                new ReissueProvisioningCodeCommand(accountId, organizationId, id), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (EmployeeNotFoundException)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, type: EmployeeNotFoundType, title: "Employee not found");
        }
        catch (EmployeeForbiddenException)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: EmployeeForbiddenType, title: "Forbidden");
        }
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }
}

public sealed record AddEmployeeMemberRequest(string FirstName, string LastName, string Email);
public sealed record RoleRequest(string Role);
