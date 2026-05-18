namespace ePrevzem.Domain.Audit;

public readonly record struct AuditLogEntryId(Guid Value)
{
    public static AuditLogEntryId New() => new(Guid.NewGuid());
}
