using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Application.Pickups.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/dashboard")]
[Authorize(Roles = "OrganizationAdmin,Employee")]
public sealed class OrgDashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgDashboardController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("stats")]
    [ProducesResponseType<DashboardStatsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetDashboardStatsQuery(GetOrganizationId()), cancellationToken));

    [HttpGet("locker-occupancy")]
    [ProducesResponseType<IReadOnlyList<LockerOccupancyResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLockerOccupancy(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetLockerOccupancyQuery(GetOrganizationId()), cancellationToken));

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId ?? throw new InvalidOperationException("Organization not resolved.");
}
