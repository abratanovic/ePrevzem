using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Common.Abstractions;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogEntryResponse>> GetForOrganizationAsync(
        OrganizationId organizationId,
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogEntryResponse>> GetForAdminAsync(
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns audit entries visible to a single citizen: events the citizen performed
    /// plus package-lifecycle events about packages addressed to that citizen, restricted
    /// to the action whitelist carried on <paramref name="filter"/> (<see cref="AuditLogQueryFilter.ActionsIn"/>).
    /// </summary>
    Task<IReadOnlyList<AuditLogEntryResponse>> GetForCitizenAsync(
        CitizenUserId citizenUserId,
        AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed record AuditLogQueryFilter(
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To,
    OrganizationId? OrganizationId,
    AuditAction? Action,
    AuditTargetKind? TargetKind,
    EmployeeAccountId? ActorEmployeeAccountId = null,
    IReadOnlyCollection<AuditAction>? ActionsIn = null);
