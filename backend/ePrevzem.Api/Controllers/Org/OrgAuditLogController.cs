using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Audit.Queries;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Org;

[ApiController]
[Route("api/org/audit-log")]
[Authorize(Roles = "OrganizationAdmin,Employee")]
public sealed class OrgAuditLogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrgAuditLogController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AuditLogEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetKind = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum(action, out AuditAction? parsedAction, out var actionProblem))
            return BadRequest(actionProblem);
        if (!TryParseEnum(targetKind, out AuditTargetKind? parsedTargetKind, out var targetKindProblem))
            return BadRequest(targetKindProblem);

        return Ok(await _mediator.Send(
            new GetOrganizationAuditLogQuery(
                GetOrganizationId(),
                limit,
                from,
                to,
                parsedAction,
                parsedTargetKind),
            cancellationToken));
    }

    private Guid GetOrganizationId()
        => _currentUser.OrganizationId
            ?? throw new InvalidOperationException("Authenticated organization user is missing organization_id claim.");

    private static bool TryParseEnum<TEnum>(
        string? value,
        out TEnum? parsed,
        out ValidationProblemDetails? problem)
        where TEnum : struct
    {
        parsed = null;
        problem = null;

        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            parsed = result;
            return true;
        }

        problem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [typeof(TEnum).Name] = [$"Invalid value '{value}'."]
        });
        return false;
    }
}
