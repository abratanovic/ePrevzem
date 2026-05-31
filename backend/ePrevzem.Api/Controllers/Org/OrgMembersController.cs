using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations;
using ePrevzem.Application.Organizations.AddMember;
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

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }
}

public sealed record AddEmployeeMemberRequest(string FirstName, string LastName, string Email);
