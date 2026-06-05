using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations.GetInfo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org")]
[Authorize]
public sealed class OrgInfoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgInfoController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("info")]
    [ProducesResponseType<OrgInfoResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfo(CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Organization not resolved.");

        var response = await _mediator.Send(new GetOrgInfoQuery(organizationId), cancellationToken);
        return Ok(response);
    }
}
