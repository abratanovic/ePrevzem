using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
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

public sealed record GetCitizenAuditLogQuery(
    Guid CitizenUserId,
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<IReadOnlyList<AuditLogEntryResponse>>;

public sealed record GetOperatorAuditLogQuery(
    Guid EmployeeAccountId,
    Guid OrganizationId,
    int Limit,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<IReadOnlyList<AuditLogEntryResponse>>;

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

public sealed class GetCitizenAuditLogQueryHandler
    : IRequestHandler<GetCitizenAuditLogQuery, IReadOnlyList<AuditLogEntryResponse>>
{
    private readonly IAuditLogRepository _repository;

    public GetCitizenAuditLogQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AuditLogEntryResponse>> Handle(
        GetCitizenAuditLogQuery request,
        CancellationToken cancellationToken)
        => _repository.GetForCitizenAsync(
            new CitizenUserId(request.CitizenUserId),
            new AuditLogQueryFilter(
                Math.Clamp(request.Limit, 1, 100),
                request.From,
                request.To,
                OrganizationId: null,
                Action: null,
                TargetKind: null,
                ActorEmployeeAccountId: null,
                ActionsIn: AuditVisibility.CitizenActions),
            cancellationToken);
}

public sealed class GetOperatorAuditLogQueryHandler
    : IRequestHandler<GetOperatorAuditLogQuery, IReadOnlyList<AuditLogEntryResponse>>
{
    private readonly IAuditLogRepository _repository;

    public GetOperatorAuditLogQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AuditLogEntryResponse>> Handle(
        GetOperatorAuditLogQuery request,
        CancellationToken cancellationToken)
        => _repository.GetForOrganizationAsync(
            new OrganizationId(request.OrganizationId),
            new AuditLogQueryFilter(
                Math.Clamp(request.Limit, 1, 100),
                request.From,
                request.To,
                OrganizationId: null,
                Action: null,
                TargetKind: null,
                ActorEmployeeAccountId: new EmployeeAccountId(request.EmployeeAccountId),
                ActionsIn: AuditVisibility.OperatorActions),
            cancellationToken);
}
