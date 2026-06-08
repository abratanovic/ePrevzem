using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Audit.Queries;
using ePrevzem.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Operator;

/// <summary>
/// Personal audit feed for a locker operator (Employee role): the operator's own
/// package-handling work, scoped to their account and organization.
/// </summary>
[ApiController]
[Route("api/operator/audit-log")]
[Authorize(Roles = "Employee")]
public sealed class OperatorAuditLogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OperatorAuditLogController(IMediator mediator, ICurrentUser currentUser)
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
            new GetOperatorAuditLogQuery(GetEmployeeAccountId(), GetOrganizationId(), limit, from, to),
            cancellationToken));

    private Guid GetEmployeeAccountId()
        => _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated operator is missing subject claim.");

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Authenticated operator is missing organization_id claim.");
}
