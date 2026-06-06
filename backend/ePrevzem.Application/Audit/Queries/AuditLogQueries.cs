using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Audit.Queries;

public sealed record GetOrganizationAuditLogQuery(
    Guid OrganizationId,
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To,
    AuditAction? Action,
    AuditTargetKind? TargetKind) : IRequest<IReadOnlyList<AuditLogEntryResponse>>;

public sealed record GetAdminAuditLogQuery(
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? OrganizationId,
    AuditAction? Action,
    AuditTargetKind? TargetKind) : IRequest<IReadOnlyList<AuditLogEntryResponse>>;

public sealed class GetOrganizationAuditLogQueryHandler
    : IRequestHandler<GetOrganizationAuditLogQuery, IReadOnlyList<AuditLogEntryResponse>>
{
    private readonly IAuditLogRepository _repository;

    public GetOrganizationAuditLogQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AuditLogEntryResponse>> Handle(
        GetOrganizationAuditLogQuery request,
        CancellationToken cancellationToken)
        => _repository.GetForOrganizationAsync(
            new OrganizationId(request.OrganizationId),
            new AuditLogQueryFilter(
                Math.Clamp(request.Limit, 1, 100),
                request.From,
                request.To,
                OrganizationId: null,
                request.Action,
                request.TargetKind),
            cancellationToken);
}

public sealed class GetAdminAuditLogQueryHandler
    : IRequestHandler<GetAdminAuditLogQuery, IReadOnlyList<AuditLogEntryResponse>>
{
    private readonly IAuditLogRepository _repository;

    public GetAdminAuditLogQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AuditLogEntryResponse>> Handle(
        GetAdminAuditLogQuery request,
        CancellationToken cancellationToken)
        => _repository.GetForAdminAsync(
            new AuditLogQueryFilter(
                Math.Clamp(request.Limit, 1, 100),
                request.From,
                request.To,
                request.OrganizationId is null ? null : new OrganizationId(request.OrganizationId.Value),
                request.Action,
                request.TargetKind),
            cancellationToken);
}
