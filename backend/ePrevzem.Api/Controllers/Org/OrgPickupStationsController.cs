using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.ClaimPickupStation;
using ePrevzem.Application.Lockers.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/stations")]
[Authorize]
public sealed class OrgPickupStationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgPickupStationsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private const string StationNotFoundType = "urn:eprevzem:stations:not-found";
    private const string StationAlreadyClaimedType = "urn:eprevzem:stations:already-claimed";

    [HttpPost]
    [ProducesResponseType<StationClaimResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Claim(
        [FromBody] ClaimPickupStationRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

        try
        {
            var response = await _mediator.Send(
                new ClaimPickupStationCommand(
                    organizationId,
                    request.SerialNumber,
                    request.Latitude,
                    request.Longitude,
                    request.Address,
                    request.HouseNumber,
                    request.ZipCode,
                    request.City),
                cancellationToken);

            return CreatedAtAction(nameof(Claim), new { id = response.ClaimId }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (PickupStationNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Station not found",
                type: StationNotFoundType,
                detail: "Postaja s to serijsko številko ne obstaja.");
        }
        catch (StationAlreadyClaimedException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Station already claimed",
                type: StationAlreadyClaimedType,
                detail: "Postaja je že prijavljena pri drugi organizaciji.");
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

public sealed record ClaimPickupStationRequest(
    string SerialNumber,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string HouseNumber,
    string ZipCode,
    string City);
