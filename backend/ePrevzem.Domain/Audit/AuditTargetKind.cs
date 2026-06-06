namespace ePrevzem.Domain.Audit;

public enum AuditTargetKind
{
    Package,
    Placement,
    Delegation,
    EmployeeAccount,
    OrganizationAdminAccount,
    SystemAdmin,
    EmployeeDevice,
    CitizenUser,
    CitizenDevice,
    CitizenActivationCode,
    RefreshToken,
    Locker,
    Organization,
    PickupStation,
    StationClaim,
    ProvisioningCode
}
