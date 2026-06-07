package si.mentis.eprevzemmobile.data.logevent

/** Mirrors the backend `AuditTargetKind` enum. */
enum class LogTargetKind {
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
    ProvisioningCode,
}
