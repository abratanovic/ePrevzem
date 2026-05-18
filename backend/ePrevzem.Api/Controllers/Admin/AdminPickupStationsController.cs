using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Application.Lockers.RegisterPickupStation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stations")]
[Authorize]
public sealed class AdminPickupStationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPickupStationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private const string DuplicateSerialNumberType = "urn:eprevzem:stations:duplicate-serial-number";

    [HttpPost]
    [ProducesResponseType<PickupStationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPickupStationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RegisterPickupStationCommand(request.SerialNumber, request.LockerNumbers),
                cancellationToken);

            return CreatedAtAction(nameof(Register), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (DuplicateSerialNumberException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate serial number",
                type: DuplicateSerialNumberType,
                detail: "Postaja s to serijsko številko že obstaja.");
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

public sealed record RegisterPickupStationRequest(
    string SerialNumber,
    IReadOnlyList<int> LockerNumbers);
