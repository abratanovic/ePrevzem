using System.Text.Json;
using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Application.Audit;

public sealed class AuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUser _currentUser;

    public AuditLogWriter(IAuditLogRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task RecordAsync(
        IDomainEvent domainEvent,
        AuditActor actor,
        OrganizationId? organizationId,
        AuditAction action,
        AuditTargetKind targetKind,
        Guid targetId,
        AuditLogDetailsResponse? details,
        CancellationToken cancellationToken)
    {
        var entry = AuditLogEntry.Record(
            AuditLogEntryId.New(),
            domainEvent.OccurredOn,
            actor.Kind,
            actor.CitizenUserId,
            actor.EmployeeAccountId,
            actor.OrganizationAdminAccountId,
            actor.SystemAdminId,
            organizationId,
            action,
            targetKind,
            targetId,
            details is null ? null : JsonSerializer.Serialize(details, JsonOptions));

        return _repository.AddAsync(entry, cancellationToken);
    }

    public AuditActor CurrentActorOrSystem()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return AuditActor.System();

        if (_currentUser.IsInRole("OrganizationAdmin"))
            return AuditActor.OrganizationAdmin(new OrganizationAdminAccountId(_currentUser.UserId.Value));
        if (_currentUser.IsInRole("Employee"))
            return AuditActor.Employee(new EmployeeAccountId(_currentUser.UserId.Value));
        if (_currentUser.IsInRole("SystemAdmin"))
            return AuditActor.SystemAdmin(new SystemAdminId(_currentUser.UserId.Value));

        return AuditActor.System();
    }
}

public sealed record AuditActor(
    AuditActorKind Kind,
    CitizenUserId? CitizenUserId,
    EmployeeAccountId? EmployeeAccountId,
    OrganizationAdminAccountId? OrganizationAdminAccountId,
    SystemAdminId? SystemAdminId)
{
    public static AuditActor Citizen(CitizenUserId id) => new(AuditActorKind.Citizen, id, null, null, null);
    public static AuditActor Employee(EmployeeAccountId id) => new(AuditActorKind.Employee, null, id, null, null);
    public static AuditActor OrganizationAdmin(OrganizationAdminAccountId id) => new(AuditActorKind.OrganizationAdmin, null, null, id, null);
    public static AuditActor SystemAdmin(SystemAdminId id) => new(AuditActorKind.SystemAdmin, null, null, null, id);
    public static AuditActor System() => new(AuditActorKind.System, null, null, null, null);
}
