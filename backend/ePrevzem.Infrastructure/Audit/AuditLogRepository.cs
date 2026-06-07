using System.Text.Json;
using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Audit;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EPrevzemDbContext _dbContext;

    public AuditLogRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        => _dbContext.AuditLogEntries.AddAsync(entry, cancellationToken).AsTask();

    public async Task<IReadOnlyList<AuditLogEntryResponse>> GetForOrganizationAsync(
        OrganizationId organizationId,
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default)
        => await ProjectAsync(
            ApplyFilter(_dbContext.AuditLogEntries.AsNoTracking(), filter with { OrganizationId = organizationId }),
            filter.Limit,
            cancellationToken);

    public async Task<IReadOnlyList<AuditLogEntryResponse>> GetForAdminAsync(
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default)
        => await ProjectAsync(
            ApplyFilter(_dbContext.AuditLogEntries.AsNoTracking(), filter),
            filter.Limit,
            cancellationToken);

    public async Task<IReadOnlyList<AuditLogEntryResponse>> GetForCitizenAsync(
        CitizenUserId citizenUserId,
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var ownPackageIds = _dbContext.Packages
            .AsNoTracking()
            .Where(p => p.RecipientCitizenUserId == citizenUserId)
            .Select(p => p.Id.Value);

        var query = ApplyFilter(_dbContext.AuditLogEntries.AsNoTracking(), filter)
            .Where(x => x.ActorCitizenUserId == citizenUserId
                || (x.TargetKind == AuditTargetKind.Package && ownPackageIds.Contains(x.TargetId)));

        return await ProjectAsync(query, filter.Limit, cancellationToken);
    }

    private static IQueryable<AuditLogEntry> ApplyFilter(
        IQueryable<AuditLogEntry> query,
        AuditLogQueryFilter filter)
    {
        if (filter.OrganizationId is not null)
            query = query.Where(x => x.OrganizationId == filter.OrganizationId.Value);
        if (filter.From is not null)
            query = query.Where(x => x.OccurredAt >= filter.From.Value);
        if (filter.To is not null)
            query = query.Where(x => x.OccurredAt <= filter.To.Value);
        if (filter.Action is not null)
            query = query.Where(x => x.Action == filter.Action.Value);
        if (filter.TargetKind is not null)
            query = query.Where(x => x.TargetKind == filter.TargetKind.Value);
        if (filter.ActorEmployeeAccountId is not null)
            query = query.Where(x => x.ActorEmployeeAccountId == filter.ActorEmployeeAccountId.Value);
        if (filter.ActionsIn is { Count: > 0 })
            query = query.Where(x => filter.ActionsIn.Contains(x.Action));

        return query;
    }

    private static async Task<IReadOnlyList<AuditLogEntryResponse>> ProjectAsync(
        IQueryable<AuditLogEntry> query,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new AuditLogEntryResponse(
            x.Id.Value,
            x.OccurredAt,
            x.ActorKind.ToString(),
            x.ActorCitizenUserId?.Value,
            x.ActorEmployeeAccountId?.Value,
            x.ActorOrganizationAdminAccountId?.Value,
            x.ActorSystemAdminId?.Value,
            x.OrganizationId?.Value,
            x.Action.ToString(),
            x.TargetKind.ToString(),
            x.TargetId,
            DeserializeDetails(x.Details))).ToList();
    }

    private static AuditLogDetailsResponse? DeserializeDetails(string? details)
        => string.IsNullOrWhiteSpace(details)
            ? null
            : JsonSerializer.Deserialize<AuditLogDetailsResponse>(details, JsonOptions);
}
