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

    [HttpPost]
    [ProducesResponseType<OrganizationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new CreateOrganizationCommand(
                    request.Name,
                    request.TaxNumber,
                    request.RegistrationNumber,
                    request.DefaultPickupDuration),
                cancellationToken);

            return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
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

public sealed record CreateOrganizationRequest(
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    TimeSpan DefaultPickupDuration);
