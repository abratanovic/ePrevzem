using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Audit;

public sealed class AuditLogEntry : AggregateRoot<AuditLogEntryId>
{
    public DateTimeOffset OccurredAt { get; private set; }
    public AuditActorKind ActorKind { get; private set; }
    public CitizenUserId? ActorCitizenUserId { get; private set; }
    public EmployeeAccountId? ActorEmployeeAccountId { get; private set; }
    public SystemAdminId? ActorSystemAdminId { get; private set; }
    public OrganizationId? OrganizationId { get; private set; }
    public AuditAction Action { get; private set; }
    public AuditTargetKind TargetKind { get; private set; }
    public Guid TargetId { get; private set; }
    public string? Details { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Record(
        AuditLogEntryId id,
        DateTimeOffset occurredAt,
        AuditActorKind actorKind,
        CitizenUserId? actorCitizenUserId,
        EmployeeAccountId? actorEmployeeAccountId,
        SystemAdminId? actorSystemAdminId,
        OrganizationId? organizationId,
        AuditAction action,
        AuditTargetKind targetKind,
        Guid targetId,
        string? details)
    {
        ValidateActor(actorKind, actorCitizenUserId, actorEmployeeAccountId, actorSystemAdminId);

        return new AuditLogEntry
        {
            Id = id,
            OccurredAt = occurredAt,
            ActorKind = actorKind,
            ActorCitizenUserId = actorCitizenUserId,
            ActorEmployeeAccountId = actorEmployeeAccountId,
            ActorSystemAdminId = actorSystemAdminId,
            OrganizationId = organizationId,
            Action = action,
            TargetKind = targetKind,
            TargetId = targetId,
            Details = details
        };
    }

    private static void ValidateActor(
        AuditActorKind kind,
        CitizenUserId? citizenId,
        EmployeeAccountId? employeeId,
        SystemAdminId? adminId)
    {
        var providedCount = (citizenId is null ? 0 : 1) + (employeeId is null ? 0 : 1) + (adminId is null ? 0 : 1);

        if (kind == AuditActorKind.System)
        {
            if (providedCount != 0)
                throw new ArgumentException("System actor must not carry any actor id.");
            return;
        }

        if (providedCount != 1)
            throw new ArgumentException($"Actor kind {kind} requires exactly one matching actor id.");

        switch (kind)
        {
            case AuditActorKind.Citizen when citizenId is null:
                throw new ArgumentException("Citizen actor requires ActorCitizenUserId.");
            case AuditActorKind.Employee when employeeId is null:
                throw new ArgumentException("Employee actor requires ActorEmployeeAccountId.");
            case AuditActorKind.SystemAdmin when adminId is null:
                throw new ArgumentException("SystemAdmin actor requires ActorSystemAdminId.");
        }
    }
}
