using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.ClaimPickupStation;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Application.Lockers.OrganizationPickupStations;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/stations")]
[Authorize(Roles = "OrganizationAdmin")]
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

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrganizationPickupStationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var response = await _mediator.Send(
            new GetOrganizationPickupStationsQuery(organizationId),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{claimId:guid}")]
    [ProducesResponseType<OrganizationPickupStationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid claimId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new GetOrganizationPickupStationQuery(GetOrganizationId(), claimId),
                cancellationToken);
            return Ok(response);
        }
        catch (OrganizationPickupStationNotFoundException)
        {
            return StationNotFound();
        }
    }

    [HttpPost]
    [ProducesResponseType<StationClaimResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Claim(
        [FromBody] ClaimPickupStationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new ClaimPickupStationCommand(
                    GetOrganizationId(),
                    request.SerialNumber,
                    request.Latitude,
                    request.Longitude,
                    request.Address,
                    request.HouseNumber,
                    request.ZipCode,
                    request.City),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { claimId = response.ClaimId }, response);
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

    [HttpPut("{claimId:guid}")]
    [ProducesResponseType<OrganizationPickupStationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(
        [FromRoute] Guid claimId,
        [FromBody] UpdatePickupStationLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new UpdateOrganizationPickupStationLocationCommand(
                    GetOrganizationId(),
                    claimId,
                    request.Latitude,
                    request.Longitude,
                    request.Address,
                    request.HouseNumber,
                    request.ZipCode,
                    request.City),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (OrganizationPickupStationNotFoundException)
        {
            return StationNotFound();
        }
    }

    [HttpPut("{claimId:guid}/lockers/{lockerId:guid}/serviceability")]
    [ProducesResponseType<OrganizationPickupStationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLockerServiceability(
        [FromRoute] Guid claimId,
        [FromRoute] Guid lockerId,
        [FromBody] UpdateLockerServiceabilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new UpdateOrganizationLockerServiceabilityCommand(
                    GetOrganizationId(),
                    claimId,
                    lockerId,
                    request.IsServiceable),
                cancellationToken);
            return Ok(response);
        }
        catch (OrganizationPickupStationNotFoundException)
        {
            return StationNotFound();
        }
        catch (OrganizationLockerNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Locker not found",
                type: "urn:eprevzem:stations:locker-not-found",
                detail: "Predalček ni bil najden.");
        }
    }

    [HttpDelete("{claimId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Release([FromRoute] Guid claimId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(
                new ReleaseOrganizationPickupStationCommand(GetOrganizationId(), claimId),
                cancellationToken);
            return NoContent();
        }
        catch (OrganizationPickupStationNotFoundException)
        {
            return StationNotFound();
        }
    }

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

    private IActionResult StationNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Station not found",
            type: StationNotFoundType,
            detail: "Paketomat ni bil najden.");

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

public sealed record UpdatePickupStationLocationRequest(
    decimal Latitude,
    decimal Longitude,
    string Address,
    string HouseNumber,
    string ZipCode,
    string City);

public sealed record UpdateLockerServiceabilityRequest(bool IsServiceable);
