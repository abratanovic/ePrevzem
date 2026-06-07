using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Application.Pickups.Insert;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

/// <summary>
/// Operator-driven package insertion via the mobile app: scan a station serial,
/// see the packages awaiting placement there and the free lockers, open a
/// locker, and confirm the placement. Operator role is enforced in the handlers
/// (<c>CanOperateLockers</c>); org scope comes from the JWT.
/// </summary>
[ApiController]
[Route("api/org/insertion")]
[Authorize(Roles = "Employee")]
public sealed class OrgInsertionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgInsertionController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("context")]
    [ProducesResponseType<InsertionContextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContext(
        [FromQuery] string serial,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new GetInsertionContextQuery(GetOrganizationId(), serial), cancellationToken));
        }
        catch (InsertionStationNotFoundException)
        {
            return StationNotFound();
        }
    }

    [HttpPost("{packageId:guid}/open")]
    [ProducesResponseType<LockerTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Open(
        [FromRoute] Guid packageId,
        [FromBody] InsertionLockerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new OpenInsertionLockerCommand(GetOrganizationId(), GetActorId(), packageId, request.LockerId),
                cancellationToken));
        }
        catch (InsertionForbiddenException) { return Forbidden(); }
        catch (PickupNotFoundException) { return PackageNotFound(); }
        catch (LockerUnavailableException) { return LockerUnavailable(); }
        catch (LockerOpenException) { return LockerOpenFailed(); }
    }

    [HttpPost("{packageId:guid}/confirm")]
    [ProducesResponseType<InsertionConfirmedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid packageId,
        [FromBody] InsertionLockerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new ConfirmInsertionCommand(GetOrganizationId(), GetActorId(), packageId, request.LockerId),
                cancellationToken));
        }
        catch (InsertionForbiddenException) { return Forbidden(); }
        catch (PickupNotFoundException) { return PackageNotFound(); }
        catch (LockerUnavailableException) { return LockerUnavailable(); }
    }

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId ?? throw new InvalidOperationException("Organization not resolved.");

    private Guid GetActorId()
        => _currentUser.UserId ?? throw new InvalidOperationException("User not authenticated.");

    private IActionResult StationNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Station not found",
            detail: "Paketnik ni bil najden ali ni dodeljen vaši organizaciji.");

    private IActionResult Forbidden()
        => Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Insertion forbidden",
            detail: "Za vlaganje paketov potrebujete vlogo operaterja.");

    private IActionResult PackageNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Package not found",
            detail: "Paket ni bil najden.");

    private IActionResult LockerUnavailable()
        => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Locker unavailable",
            detail: "Izbrani predalček ni na voljo. Izberite drug predalček.");

    private IActionResult LockerOpenFailed()
        => Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Locker open failed",
            detail: "Predalčka ni bilo mogoče odpreti. Poskusite znova.");
}

public sealed record InsertionLockerRequest(Guid LockerId);
