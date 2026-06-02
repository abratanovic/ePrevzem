using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Create;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Application.Pickups.LookupRecipient;
using ePrevzem.Application.Pickups.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/pickups")]
[Authorize(Roles = "OrganizationAdmin,Employee")]
public sealed class OrgPickupsController : ControllerBase
{
    private const string RecipientNotFoundType = "urn:eprevzem:pickups:recipient-not-found";
    private const string StationForbiddenType = "urn:eprevzem:pickups:station-forbidden";
    private const string CreationForbiddenType = "urn:eprevzem:pickups:creation-forbidden";
    private const string PickupNotFoundType = "urn:eprevzem:pickups:not-found";
    private const string DeletionForbiddenType = "urn:eprevzem:pickups:deletion-forbidden";

    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgPickupsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("recipient-lookup")]
    [ProducesResponseType<PickupRecipientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupRecipient(
        [FromBody] LookupRecipientRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new LookupPickupRecipientQuery(request.Emso), cancellationToken));
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (PickupRecipientNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                type: RecipientNotFoundType,
                title: "Recipient not found",
                detail: "Prejemnik ni registriran. Pred izdelavo prevzema mora opraviti registracijo prek SI-TRUST.");
        }
    }

    [HttpGet("stations")]
    [ProducesResponseType<IReadOnlyList<PickupStationOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStationOptions(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetPickupStationOptionsQuery(GetOrganizationId()), cancellationToken));

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PickupResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetRecentPickupsQuery(GetOrganizationId(), limit), cancellationToken));

    [HttpPost]
    [ProducesResponseType<PickupResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePickupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new CreatePickupCommand(
                    GetOrganizationId(),
                    GetActorId(),
                    GetActorRole(),
                    request.RecipientEmso,
                    request.TargetPickupStationId,
                    request.Description),
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (PickupRecipientNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                type: RecipientNotFoundType,
                title: "Recipient not found",
                detail: "Prejemnik ni registriran.");
        }
        catch (PickupStationForbiddenException)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                type: StationForbiddenType,
                title: "Station forbidden",
                detail: "Izbrani paketomat ni na voljo vaši organizaciji.");
        }
        catch (PickupCreationForbiddenException)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                type: CreationForbiddenType,
                title: "Pickup creation forbidden",
                detail: "Nimate pravic za izdelavo prevzema.");
        }
    }

    [HttpDelete("{pickupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] Guid pickupId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeletePickupCommand(GetOrganizationId(), pickupId), cancellationToken);
            return NoContent();
        }
        catch (PickupNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                type: PickupNotFoundType,
                title: "Pickup not found",
                detail: "Prevzem ni bil najden.");
        }
        catch (PickupDeletionForbiddenException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                type: DeletionForbiddenType,
                title: "Pickup cannot be deleted",
                detail: "Izbrisati je mogoče samo prevzeme, ki še čakajo na vložitev v paketomat.");
        }
    }

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId ?? throw new InvalidOperationException("Organization not resolved.");

    private Guid GetActorId()
        => _currentUser.UserId ?? throw new InvalidOperationException("User not authenticated.");

    private string GetActorRole()
        => _currentUser.IsInRole("OrganizationAdmin") ? "OrganizationAdmin" : "Employee";

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }
}

public sealed record LookupRecipientRequest(string Emso);
public sealed record CreatePickupRequest(string RecipientEmso, Guid TargetPickupStationId, string Description);
