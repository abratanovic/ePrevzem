using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Audit.Queries;
using ePrevzem.Domain.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Roles = "SystemAdmin")]
public sealed class AdminAuditLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AuditLogEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetKind = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum(action, out AuditAction? parsedAction, out var actionProblem))
            return BadRequest(actionProblem);
        if (!TryParseEnum(targetKind, out AuditTargetKind? parsedTargetKind, out var targetKindProblem))
            return BadRequest(targetKindProblem);

        return Ok(await _mediator.Send(
            new GetAdminAuditLogQuery(
                limit,
                from,
                to,
                organizationId,
                parsedAction,
                parsedTargetKind),
            cancellationToken));
    }

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
