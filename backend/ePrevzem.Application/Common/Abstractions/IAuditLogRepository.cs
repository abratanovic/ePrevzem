using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Domain.Audit;
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
}

public sealed record AuditLogQueryFilter(
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To,
    OrganizationId? OrganizationId,
    AuditAction? Action,
    AuditTargetKind? TargetKind);
