namespace ePrevzem.Application.Audit.Dtos;

public sealed record AuditLogEntryResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorKind,
    Guid? ActorCitizenUserId,
    Guid? ActorEmployeeAccountId,
    Guid? ActorOrganizationAdminAccountId,
    Guid? ActorSystemAdminId,
    Guid? OrganizationId,
    string Action,
    string TargetKind,
    Guid TargetId,
    AuditLogDetailsResponse? Details);

public sealed record AuditLogDetailsResponse(
    string? DocumentTitle = null,
    string? OrganizationName = null,
    string? LockerLabel = null,
    string? Location = null);
