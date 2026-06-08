namespace ePrevzem.Domain.Audit;
// komentar za resolvanje merge konflikta
public enum AuditAction
{
    // Packages & placements
    PackageCreated,
    PackagePlaced,
    PackagePickedUpByCitizen,
    PackageRemovedByEmployee,
    PackageExpired,
    PackageRetrievedAfterExpiry,
    PackageMarkedPickedUpManually,
    PackageCancelled,
    PackageDeleted,

    // Delegations
    DelegationCreated,
    DelegationRevoked,
    DelegationUsedAtPickup,

    // Employees, devices, codes
    ProvisioningCodeIssued,
    ProvisioningCodeRedeemed,
    EmployeeAccountCreated,
    EmployeeAccountDisabled,
    EmployeeAccountReenabled,
    EmployeeAccountLoggedIn,
    EmployeePasswordChanged,
    EmployeeAccountRoleGranted,
    EmployeeAccountRoleRevoked,
    EmployeeStationAccessGranted,
    EmployeeStationAccessRevoked,
    EmployeeDeviceRegistered,
    EmployeeDeviceRevoked,
    CitizenActivationCodeIssued,
    CitizenDeviceRegistered,
    CitizenDeviceRevoked,
    OrganizationAdminAccountCreated,
    OrganizationAdminAccountDisabled,
    OrganizationAdminAccountReenabled,
    OrganizationAdminLoggedIn,
    OrganizationAdminPasswordChanged,

    // Citizens
    CitizenOnboarded,

    // Tenancy & infrastructure
    OrganizationCreated,
    SystemAdminLoggedIn,
    SystemAdminLoginFailed,
    SystemAdminPasswordChanged,
    RefreshTokenRotated,
    RefreshTokenChainRevoked,
    PickupStationCreated,
    StationClaimed,
    StationReleased,
    LockerCreated,
    LockerServiceabilityChanged,
    LockerOpened
}
