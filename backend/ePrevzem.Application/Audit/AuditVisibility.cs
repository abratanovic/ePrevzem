using ePrevzem.Domain.Audit;

namespace ePrevzem.Application.Audit;

/// <summary>
/// Visibility policy for audit entries surfaced to the mobile clients. Defines which
/// <see cref="AuditAction"/> values each non-admin audience is allowed to see.
/// </summary>
public static class AuditVisibility
{
    /// <summary>
    /// Actions a citizen may see about the documents addressed to them: the package
    /// lifecycle plus their delegations.
    /// </summary>
    public static readonly IReadOnlyCollection<AuditAction> CitizenActions = new[]
    {
        AuditAction.PackageCreated,
        AuditAction.PackagePlaced,
        AuditAction.PackagePickedUpByCitizen,
        AuditAction.PackageRemovedByEmployee,
        AuditAction.PackageExpired,
        AuditAction.PackageRetrievedAfterExpiry,
        AuditAction.PackageMarkedPickedUpManually,
        AuditAction.PackageCancelled,
        AuditAction.PackageDeleted,
        AuditAction.DelegationCreated,
        AuditAction.DelegationRevoked,
        AuditAction.DelegationUsedAtPickup,
    };

    /// <summary>
    /// Actions a locker operator may see for their own work: placing documents into and
    /// removing them from lockers.
    /// </summary>
    public static readonly IReadOnlyCollection<AuditAction> OperatorActions = new[]
    {
        AuditAction.PackagePlaced,
        AuditAction.PackageRemovedByEmployee,
        AuditAction.PackageRetrievedAfterExpiry,
        AuditAction.PackageMarkedPickedUpManually,
        AuditAction.LockerOpened,
    };
}
