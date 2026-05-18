namespace ePrevzem.Domain.Audit;

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
    EmployeeAccountRoleGranted,
    EmployeeAccountRoleRevoked,
    EmployeeStationAccessGranted,
    EmployeeStationAccessRevoked,
    EmployeeDeviceRegistered,
    EmployeeDeviceRevoked,
    CitizenDeviceRegistered,
    CitizenDeviceRevoked,

    // Citizens
    CitizenOnboarded,

    // Tenancy & infrastructure
    OrganizationCreated,
    StationClaimed,
    StationReleased,
    LockerCreated,
    LockerServiceabilityChanged,
    LockerOpened
}
