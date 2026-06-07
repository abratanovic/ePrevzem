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

    public async Task<IReadOnlyList<AuditActorOptionResponse>> GetActorOptionsForOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Where(x => x.ActorKind != AuditActorKind.System)
            .Select(x => new ActorRow(
                x.ActorKind,
                x.ActorCitizenUserId,
                x.ActorEmployeeAccountId,
                x.ActorOrganizationAdminAccountId,
                x.ActorSystemAdminId))
            .ToListAsync(cancellationToken);

        var actors = rows
            .Distinct()
            .Where(x => ActorIdFor(x) is not null)
            .ToList();
        var actorDisplays = await LoadActorDisplaysAsync(actors, cancellationToken);

        return actors
            .Select(x =>
            {
                var actorDisplay = GetActorDisplay(x, actorDisplays);
                return new AuditActorOptionResponse(
                    x.ActorKind.ToString(),
                    ActorIdFor(x)!.Value,
                    actorDisplay.DisplayName,
                    actorDisplay.Email);
            })
            .OrderBy(x => x.DisplayName ?? x.Email ?? x.ActorId.ToString())
            .ToList();
    }

    public async Task<IReadOnlyList<AuditLogEntryResponse>> GetForCitizenAsync(
        CitizenUserId citizenUserId,
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Materialise the citizen's package ids first. EF Core cannot translate a value-object
        // key projection (p.Id.Value) inside a Contains subquery against TargetId, so we pull the
        // ids into memory and use a translatable List<Guid>.Contains (rendered as SQL = ANY).
        var ownPackageIds = await _dbContext.Packages
            .AsNoTracking()
            .Where(p => p.RecipientCitizenUserId == citizenUserId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var ownPackageGuids = ownPackageIds.Select(id => id.Value).ToList();

        var query = ApplyFilter(_dbContext.AuditLogEntries.AsNoTracking(), filter)
            .Where(x => x.ActorCitizenUserId == citizenUserId
                || (x.TargetKind == AuditTargetKind.Package && ownPackageGuids.Contains(x.TargetId)));

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
        if (filter.ActorKind is not null && filter.ActorId is not null)
        {
            var actorId = filter.ActorId.Value;
            query = filter.ActorKind.Value switch
            {
                AuditActorKind.Citizen => query.Where(x => x.ActorCitizenUserId != null && x.ActorCitizenUserId.Value.Value == actorId),
                AuditActorKind.Employee => query.Where(x => x.ActorEmployeeAccountId != null && x.ActorEmployeeAccountId.Value.Value == actorId),
                AuditActorKind.OrganizationAdmin => query.Where(x => x.ActorOrganizationAdminAccountId != null && x.ActorOrganizationAdminAccountId.Value.Value == actorId),
                AuditActorKind.SystemAdmin => query.Where(x => x.ActorSystemAdminId != null && x.ActorSystemAdminId.Value.Value == actorId),
                _ => query
            };
        }
        if (filter.ActionsIn is { Count: > 0 })
            query = query.Where(x => filter.ActionsIn.Contains(x.Action));

        return query;
    }

    private async Task<IReadOnlyList<AuditLogEntryResponse>> ProjectAsync(
        IQueryable<AuditLogEntry> query,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var actorDisplays = await LoadActorDisplaysAsync(rows, cancellationToken);

        return rows.Select(x =>
        {
            var actorDisplay = GetActorDisplay(x, actorDisplays);

            return new AuditLogEntryResponse(
                x.Id.Value,
                x.OccurredAt,
                x.ActorKind.ToString(),
                actorDisplay.DisplayName,
                actorDisplay.Email,
                x.ActorCitizenUserId?.Value,
                x.ActorEmployeeAccountId?.Value,
                x.ActorOrganizationAdminAccountId?.Value,
                x.ActorSystemAdminId?.Value,
                x.OrganizationId?.Value,
                x.Action.ToString(),
                x.TargetKind.ToString(),
                x.TargetId,
                DeserializeDetails(x.Details));
        }).ToList();
    }

    private async Task<ActorDisplays> LoadActorDisplaysAsync(
        IReadOnlyCollection<AuditLogEntry> rows,
        CancellationToken cancellationToken)
    {
        var citizenIds = rows
            .Select(x => x.ActorCitizenUserId)
            .OfType<CitizenUserId>()
            .Distinct()
            .ToList();
        var employeeIds = rows
            .Select(x => x.ActorEmployeeAccountId)
            .OfType<EmployeeAccountId>()
            .Distinct()
            .ToList();
        var organizationAdminIds = rows
            .Select(x => x.ActorOrganizationAdminAccountId)
            .OfType<OrganizationAdminAccountId>()
            .Distinct()
            .ToList();
        var systemAdminIds = rows
            .Select(x => x.ActorSystemAdminId)
            .OfType<SystemAdminId>()
            .Distinct()
            .ToList();

        var citizens = citizenIds.Count == 0
            ? new Dictionary<CitizenUserId, ActorDisplay>()
            : await _dbContext.CitizenUsers
                .AsNoTracking()
                .Where(x => citizenIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var employees = employeeIds.Count == 0
            ? new Dictionary<EmployeeAccountId, ActorDisplay>()
            : await _dbContext.EmployeeAccounts
                .AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var organizationAdmins = organizationAdminIds.Count == 0
            ? new Dictionary<OrganizationAdminAccountId, ActorDisplay>()
            : await _dbContext.OrganizationAdminAccounts
                .AsNoTracking()
                .Where(x => organizationAdminIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var systemAdmins = systemAdminIds.Count == 0
            ? new Dictionary<SystemAdminId, ActorDisplay>()
            : await _dbContext.SystemAdmins
                .AsNoTracking()
                .Where(x => systemAdminIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Username })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(x.Username, Email: null),
                    cancellationToken);

        return new ActorDisplays(citizens, employees, organizationAdmins, systemAdmins);
    }

    private async Task<ActorDisplays> LoadActorDisplaysAsync(
        IReadOnlyCollection<ActorRow> rows,
        CancellationToken cancellationToken)
    {
        var citizenIds = rows
            .Select(x => x.ActorCitizenUserId)
            .OfType<CitizenUserId>()
            .Distinct()
            .ToList();
        var employeeIds = rows
            .Select(x => x.ActorEmployeeAccountId)
            .OfType<EmployeeAccountId>()
            .Distinct()
            .ToList();
        var organizationAdminIds = rows
            .Select(x => x.ActorOrganizationAdminAccountId)
            .OfType<OrganizationAdminAccountId>()
            .Distinct()
            .ToList();
        var systemAdminIds = rows
            .Select(x => x.ActorSystemAdminId)
            .OfType<SystemAdminId>()
            .Distinct()
            .ToList();

        var citizens = citizenIds.Count == 0
            ? new Dictionary<CitizenUserId, ActorDisplay>()
            : await _dbContext.CitizenUsers
                .AsNoTracking()
                .Where(x => citizenIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var employees = employeeIds.Count == 0
            ? new Dictionary<EmployeeAccountId, ActorDisplay>()
            : await _dbContext.EmployeeAccounts
                .AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var organizationAdmins = organizationAdminIds.Count == 0
            ? new Dictionary<OrganizationAdminAccountId, ActorDisplay>()
            : await _dbContext.OrganizationAdminAccounts
                .AsNoTracking()
                .Where(x => organizationAdminIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(FullName(x.FirstName, x.LastName), x.Email),
                    cancellationToken);

        var systemAdmins = systemAdminIds.Count == 0
            ? new Dictionary<SystemAdminId, ActorDisplay>()
            : await _dbContext.SystemAdmins
                .AsNoTracking()
                .Where(x => systemAdminIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Username })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ActorDisplay(x.Username, Email: null),
                    cancellationToken);

        return new ActorDisplays(citizens, employees, organizationAdmins, systemAdmins);
    }

    private static ActorDisplay GetActorDisplay(AuditLogEntry entry, ActorDisplays actorDisplays)
    {
        return entry.ActorKind switch
        {
            AuditActorKind.Citizen when entry.ActorCitizenUserId is not null
                => actorDisplays.Citizens.GetValueOrDefault(entry.ActorCitizenUserId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.Employee when entry.ActorEmployeeAccountId is not null
                => actorDisplays.Employees.GetValueOrDefault(entry.ActorEmployeeAccountId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.OrganizationAdmin when entry.ActorOrganizationAdminAccountId is not null
                => actorDisplays.OrganizationAdmins.GetValueOrDefault(entry.ActorOrganizationAdminAccountId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.SystemAdmin when entry.ActorSystemAdminId is not null
                => actorDisplays.SystemAdmins.GetValueOrDefault(entry.ActorSystemAdminId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.System => new ActorDisplay("Sistem", Email: null),
            _ => ActorDisplay.Empty
        };
    }

    private static ActorDisplay GetActorDisplay(ActorRow row, ActorDisplays actorDisplays)
    {
        return row.ActorKind switch
        {
            AuditActorKind.Citizen when row.ActorCitizenUserId is not null
                => actorDisplays.Citizens.GetValueOrDefault(row.ActorCitizenUserId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.Employee when row.ActorEmployeeAccountId is not null
                => actorDisplays.Employees.GetValueOrDefault(row.ActorEmployeeAccountId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.OrganizationAdmin when row.ActorOrganizationAdminAccountId is not null
                => actorDisplays.OrganizationAdmins.GetValueOrDefault(row.ActorOrganizationAdminAccountId.Value) ?? ActorDisplay.Empty,
            AuditActorKind.SystemAdmin when row.ActorSystemAdminId is not null
                => actorDisplays.SystemAdmins.GetValueOrDefault(row.ActorSystemAdminId.Value) ?? ActorDisplay.Empty,
            _ => ActorDisplay.Empty
        };
    }

    private static Guid? ActorIdFor(ActorRow row)
    {
        return row.ActorKind switch
        {
            AuditActorKind.Citizen => row.ActorCitizenUserId?.Value,
            AuditActorKind.Employee => row.ActorEmployeeAccountId?.Value,
            AuditActorKind.OrganizationAdmin => row.ActorOrganizationAdminAccountId?.Value,
            AuditActorKind.SystemAdmin => row.ActorSystemAdminId?.Value,
            _ => null
        };
    }

    private static string FullName(string firstName, string lastName)
        => $"{firstName} {lastName}".Trim();

    private static AuditLogDetailsResponse? DeserializeDetails(string? details)
        => string.IsNullOrWhiteSpace(details)
            ? null
            : JsonSerializer.Deserialize<AuditLogDetailsResponse>(details, JsonOptions);

    private sealed record ActorDisplay(string? DisplayName, string? Email)
    {
        public static readonly ActorDisplay Empty = new(DisplayName: null, Email: null);
    }

    private sealed record ActorDisplays(
        IReadOnlyDictionary<CitizenUserId, ActorDisplay> Citizens,
        IReadOnlyDictionary<EmployeeAccountId, ActorDisplay> Employees,
        IReadOnlyDictionary<OrganizationAdminAccountId, ActorDisplay> OrganizationAdmins,
        IReadOnlyDictionary<SystemAdminId, ActorDisplay> SystemAdmins);

    private sealed record ActorRow(
        AuditActorKind ActorKind,
        CitizenUserId? ActorCitizenUserId,
        EmployeeAccountId? ActorEmployeeAccountId,
        OrganizationAdminAccountId? ActorOrganizationAdminAccountId,
        SystemAdminId? ActorSystemAdminId);
}
