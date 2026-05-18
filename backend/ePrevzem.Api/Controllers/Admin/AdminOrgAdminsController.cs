using ePrevzem.Application.Identity.CreateOrgAdmin;
using ePrevzem.Application.Identity.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/org-admins")]
[Authorize]
public sealed class AdminOrgAdminsController : ControllerBase
{
    private const string DuplicateEmailType = "urn:eprevzem:identity:duplicate-org-admin-email";
    private const string OrgNotFoundType = "urn:eprevzem:identity:organization-not-found";

    private readonly IMediator _mediator;

    public AdminOrgAdminsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType<CreateOrganizationAdminResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationAdminRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new CreateOrganizationAdminCommand(
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.OrganizationId),
                cancellationToken);

            return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (OrganizationNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Organization not found",
                type: OrgNotFoundType,
                detail: "Organizacija ne obstaja.");
        }
        catch (DuplicateOrgAdminEmailException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate email",
                type: DuplicateEmailType,
                detail: "Organizacijski administrator s tem e-poštnim naslovom že obstaja.");
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

public sealed record CreateOrganizationAdminRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid OrganizationId);
