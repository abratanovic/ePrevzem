using ePrevzem.Application.Organizations.Create;
using ePrevzem.Application.Organizations.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/organizations")]
[Authorize]
public sealed class AdminOrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminOrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private const string DuplicateTaxNumberType = "urn:eprevzem:organizations:duplicate-tax-number";
    private const string DuplicateRegistrationNumberType = "urn:eprevzem:organizations:duplicate-registration-number";

    [HttpPost]
    [ProducesResponseType<OrganizationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new CreateOrganizationCommand(
                    request.Name,
                    request.TaxNumber,
                    request.RegistrationNumber,
                    request.DefaultPickupDurationDays),
                cancellationToken);

            return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (DuplicateTaxNumberException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate tax number",
                type: DuplicateTaxNumberType,
                detail: "Organizacija s to davčno številko že obstaja.");
        }
        catch (DuplicateRegistrationNumberException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate registration number",
                type: DuplicateRegistrationNumberType,
                detail: "Organizacija s to matično številko že obstaja.");
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

public sealed record CreateOrganizationRequest(
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    int DefaultPickupDurationDays);
