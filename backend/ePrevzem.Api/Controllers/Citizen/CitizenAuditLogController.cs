using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Audit.Queries;
using ePrevzem.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Citizen;

[ApiController]
[Route("api/citizen/audit-log")]
[Authorize(Roles = "Citizen")]
public sealed class CitizenAuditLogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public CitizenAuditLogController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AuditLogEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetCitizenAuditLogQuery(GetCitizenUserId(), limit, from, to),
            cancellationToken));

    private Guid GetCitizenUserId()
        => _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated citizen is missing subject claim.");
}
