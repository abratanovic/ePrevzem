export type AuditActorKind =
  | "Citizen"
  | "Employee"
  | "OrganizationAdmin"
  | "SystemAdmin"
  | "System"
  | (string & {});

export type AuditTargetKind =
  | "Package"
  | "Placement"
  | "Delegation"
  | "EmployeeAccount"
  | "OrganizationAdminAccount"
  | "SystemAdmin"
  | "EmployeeDevice"
  | "CitizenUser"
  | "CitizenDevice"
  | "CitizenActivationCode"
  | "RefreshToken"
  | "Locker"
  | "Organization"
  | "PickupStation"
  | "StationClaim"
  | "ProvisioningCode"
  | (string & {});

export type AuditAction =
  | "PackageCreated"
  | "PackagePlaced"
  | "PackagePickedUpByCitizen"
  | "PackageRemovedByEmployee"
  | "PackageExpired"
  | "PackageRetrievedAfterExpiry"
  | "PackageMarkedPickedUpManually"
  | "PackageCancelled"
  | "PackageDeleted"
  | "DelegationCreated"
  | "DelegationRevoked"
  | "DelegationUsedAtPickup"
  | "ProvisioningCodeIssued"
  | "ProvisioningCodeRedeemed"
  | "EmployeeAccountCreated"
  | "EmployeeAccountDisabled"
  | "EmployeeAccountReenabled"
  | "EmployeeAccountLoggedIn"
  | "EmployeePasswordChanged"
  | "EmployeeAccountRoleGranted"
  | "EmployeeAccountRoleRevoked"
  | "EmployeeStationAccessGranted"
  | "EmployeeStationAccessRevoked"
  | "EmployeeDeviceRegistered"
  | "EmployeeDeviceRevoked"
  | "CitizenActivationCodeIssued"
  | "CitizenDeviceRegistered"
  | "CitizenDeviceRevoked"
  | "OrganizationAdminAccountCreated"
  | "OrganizationAdminAccountDisabled"
  | "OrganizationAdminAccountReenabled"
  | "OrganizationAdminLoggedIn"
  | "OrganizationAdminPasswordChanged"
  | "CitizenOnboarded"
  | "OrganizationCreated"
  | "SystemAdminLoggedIn"
  | "SystemAdminLoginFailed"
  | "SystemAdminPasswordChanged"
  | "RefreshTokenRotated"
  | "RefreshTokenChainRevoked"
  | "PickupStationCreated"
  | "StationClaimed"
  | "StationReleased"
  | "LockerCreated"
  | "LockerServiceabilityChanged"
  | "LockerOpened"
  | (string & {});

export interface AuditLogDetails {
  documentTitle?: string | null;
  organizationName?: string | null;
  lockerLabel?: string | null;
  location?: string | null;
}

export interface AuditLogEntry {
  id: string;
  occurredAt: string;
  actorKind: AuditActorKind;
  actorDisplayName?: string | null;
  actorEmail?: string | null;
  actorCitizenUserId: string | null;
  actorEmployeeAccountId: string | null;
  actorOrganizationAdminAccountId: string | null;
  actorSystemAdminId: string | null;
  organizationId: string | null;
  action: AuditAction;
  targetKind: AuditTargetKind;
  targetId: string;
  details: AuditLogDetails | null;
}

export interface AuditLogFilters {
  limit?: number;
  from?: string;
  to?: string;
  action?: AuditAction | "";
  targetKind?: AuditTargetKind | "";
}
