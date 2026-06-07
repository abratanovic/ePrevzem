using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Application.Pickups.Open;
using ePrevzem.Application.Pickups.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Citizen;

/// <summary>
/// Citizen-facing read API for the pickups the authenticated citizen is the
/// recipient of. The citizen is resolved from the JWT <c>sub</c> claim — there
/// is no organization scope on a citizen token, so ownership is enforced by
/// filtering on the recipient id, never by a route parameter.
/// </summary>
[ApiController]
[Route("api/citizen/pickups")]
[Authorize(Roles = "Citizen")]
public sealed class CitizenPickupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public CitizenPickupsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CitizenPickupResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPickups(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetCitizenPickupsQuery(GetCitizenId()), cancellationToken));

    [HttpGet("{pickupId:guid}")]
    [ProducesResponseType<CitizenPickupDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        [FromRoute] Guid pickupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCitizenPickupDetailQuery(GetCitizenId(), pickupId),
            cancellationToken);

        return result is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Pickup not found",
                detail: "Prevzem ni bil najden.")
            : Ok(result);
    }

    [HttpPost("{pickupId:guid}/open")]
    [ProducesResponseType<LockerTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Open(
        [FromRoute] Guid pickupId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new OpenCitizenPickupCommand(GetCitizenId(), pickupId), cancellationToken));
        }
        catch (PickupNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Pickup not found",
                detail: "Prevzem ni bil najden ali ni v paketniku.");
        }
        catch (LockerOpenException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Locker open failed",
                detail: "Predalčka ni bilo mogoče odpreti. Poskusite znova.");
        }
    }

    [HttpPost("{pickupId:guid}/confirm-pickup")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPickup(
        [FromRoute] Guid pickupId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ConfirmCitizenPickupCommand(GetCitizenId(), pickupId), cancellationToken);
            return NoContent();
        }
        catch (PickupNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Pickup not found",
                detail: "Prevzem ni bil najden.");
        }
        catch (InvalidOperationException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Pickup cannot be confirmed",
                detail: "Prevzema v trenutnem stanju ni mogoče potrditi.");
        }
    }

    private Guid GetCitizenId()
        => _currentUser.UserId ?? throw new InvalidOperationException("User not authenticated.");
}
