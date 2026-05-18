# ePrevzem Backend — Domain Model Design

**Status:** Draft, pending review
**Date:** 2026-05-18
**Scope:** Domain model (entities, value objects, invariants, state machines) for the ePrevzem backend supporting the secure document pickup flow across multiple organizations and pickup stations. Authentication mechanics, transport-level concerns, hardware integration details, and infrastructure topics are referenced but not specified here.

---

## 1. Glossary

All schema, identifier, and stored enum value names are in **English**. UI copy is in **Slovenian**. This document uses English throughout. Slovenian terms from the product brief are cross-referenced where helpful.

| Term | Meaning |
|------|---------|
| Citizen | Natural person who can receive packages. Always SI-TRUST onboarded. |
| Employee | Person operating on behalf of an organization. Provisioned via a code issued by an `OrganizationAdmin`. |
| Organization | Tenant of the platform that originates packages. |
| Pickup Station (paketnik / paketomat) | Physical tower containing many lockers, at one geographic location. |
| Locker | Individual compartment within a pickup station. |
| Package (prevzem) | A single logical envelope/item the organization sends to a citizen. May internally contain any number of documents/items at the organization's discretion. |
| Placement | A specific insertion of a package into a specific locker; bounded by an insertion event and an ending event. A package can have many placements over its lifetime. |
| Delegation (pooblastilo) | Authorization for another citizen to pick up a specific package. |
| Provisioning Code | One-shot code issued by an org admin, redeemed by an employee on a new device. |
| System Admin | Operator of the ePrevzem platform itself (cross-tenant). |

## 2. Guiding principles

- **Two account worlds, kept separate.** Citizen accounts (SI-TRUST identity) and employee accounts (org-issued provisioning) are modeled as distinct aggregates with no shared base entity. Even when the same physical human holds both, the system does not link them.
- **State of the physical world is its own concern.** The lifecycle of a `Package` (business record) is decoupled from the lifecycle of a `Placement` (a particular insertion into a particular locker). A package may have zero, one, or many placements over its life.
- **Audit log is append-only and unified.** Every state-changing action emits one `AuditLogEntry`. The table is physically append-only — UPDATE/DELETE permissions are revoked at the DB level for application users.
- **Roles are sets, with one implicit superuser.** Within an organization an employee can hold any combination of `OrganizationAdmin`, `RecordManager`, and `Operator`. `OrganizationAdmin` implicitly grants the other two in code, but the implication is not represented in the data.
- **Schema today, growth tomorrow.** Where a more general structure costs little, prefer it (e.g. `StationClaim` instead of `PickupStation.OrganizationId`). Where it costs significantly more than v1 needs, defer (e.g. `OrganizationBranch` is not modeled yet — station access lists serve the purpose).
- **Persisted enums are stored as strings.** Type-safe in code, readable in raw DB rows, stable across schema changes.

## 3. Aggregates

### 3.1 Identity & accounts

```
CitizenUser {
  Id
  FirstName
  LastName
  EMSO                      -- verified via SI-TRUST onboarding; unique
  Email?
  PhoneNumber?
  OnboardedAt
}

CitizenDevice {
  Id
  CitizenUserId             -> CitizenUser.Id
  PublicKey                 -- device-generated; backend stores public key only
  DeviceFingerprint
  Label?                    -- user-facing, e.g. "iPhone 14"
  RegisteredAt
  RevokedAt?
}

EmployeeAccount {
  Id
  OrganizationId            -> Organization.Id
  FirstName
  LastName
  Email?
  Status                    -- Active | Disabled
  CreatedFromProvisioningCodeId  -> ProvisioningCode.Id
  CreatedAt
}

EmployeeAccountRole {
  EmployeeAccountId         -> EmployeeAccount.Id
  Role                      -- OrganizationAdmin | RecordManager | Operator
  PRIMARY KEY (EmployeeAccountId, Role)
}

EmployeeAccountStationAccess {
  EmployeeAccountId         -> EmployeeAccount.Id
  PickupStationId           -> PickupStation.Id
  GrantedAt
  PRIMARY KEY (EmployeeAccountId, PickupStationId)
}

EmployeeDevice {
  Id
  EmployeeAccountId         -> EmployeeAccount.Id
  PublicKey
  DeviceFingerprint
  Label?
  ProvisionedAt
  RevokedAt?
}

ProvisioningCode {
  Id
  OrganizationId            -> Organization.Id
  Code                      -- random, single-use
  PreFilledFirstName
  PreFilledLastName
  PreFilledEmail?
  Roles                     -- collection of {OrganizationAdmin, RecordManager, Operator}
  StationAccess             -- collection of PickupStationId values
  CreatedByEmployeeAccountId -> EmployeeAccount.Id
  CreatedAt
  ExpiresAt
  RedeemedAt?
  RedeemedIntoEmployeeAccountId? -> EmployeeAccount.Id
  IsReprovisioningOfEmployeeAccountId? -> EmployeeAccount.Id
                            -- null = creates new account; set = binds to existing account, auto-revokes prior device
}

SystemAdmin {
  Id
  Username
  -- credential storage out of scope for this spec
  CreatedAt
}
```

### 3.2 Tenancy & infrastructure

```
Organization {
  Id
  Name
  TaxNumber
  RegistrationNumber
  DefaultPickupDuration     -- TimeSpan; applied at placement time to compute Package.DeadlineAt
  CreatedAt
}

PickupStation {
  Id
  Location                  -- value object (see below)
  CreatedAt
  -- no direct organization FK; ownership lives in StationClaim
}

Location (value object, embedded in PickupStation) {
  Latitude
  Longitude
  Address
  HouseNumber
  ZipCode
  City
}

StationClaim {
  Id
  PickupStationId           -> PickupStation.Id
  OrganizationId            -> Organization.Id
  ClaimedAt
  ReleasedAt?
}

Locker {
  Id
  PickupStationId           -> PickupStation.Id
  LockerNumber              -- unique within station
  IsServiceable             -- bool; default true; admin-managed
}
```

### 3.3 Business records

```
Package {
  Id
  OrganizationId            -> Organization.Id
  RecipientCitizenUserId    -> CitizenUser.Id
  CreatedByEmployeeAccountId -> EmployeeAccount.Id
  TargetPickupStationId     -> PickupStation.Id     -- required at creation
  Description
  Status                    -- AwaitingPlacement | InLocker | PickedUp | NotPickedUp
                            -- | AwaitingPersonalPickup | Cancelled
  DeadlineAt?               -- null until first placement; computed at placement opening
  CreatedAt
  FinalizedAt?              -- set when Status transitions to PickedUp or Cancelled
}

Placement {
  Id
  PackageId                 -> Package.Id
  LockerId                  -> Locker.Id
  OpenedByEmployeeAccountId -> EmployeeAccount.Id
  OpenedAt
  EndedAt?
  EndReason?                -- PickedUpByCitizen | RemovedByEmployee | RetrievedAfterExpiry
  EndedByCitizenUserId?     -- set when EndReason = PickedUpByCitizen (may be recipient or active delegate)
  EndedByEmployeeAccountId? -- set when EndReason in {RemovedByEmployee, RetrievedAfterExpiry}
}

Delegation {
  Id
  PackageId                 -> Package.Id
  DelegatorCitizenUserId    -> CitizenUser.Id      -- must equal the package's recipient
  DelegateCitizenUserId     -> CitizenUser.Id
  CreatedAt
  RevokedAt?
}
```

### 3.4 Audit

```
AuditLogEntry {
  Id
  OccurredAt
  ActorKind                 -- Citizen | Employee | SystemAdmin | System
  ActorCitizenUserId?       -- exactly one actor FK set when ActorKind != System
  ActorEmployeeAccountId?
  ActorSystemAdminId?
  OrganizationId?           -- scope: enables "all events for org X" queries
  Action                    -- string-persisted enum (see §6)
  TargetKind                -- Package | Placement | Delegation | EmployeeAccount
                            -- | EmployeeDevice | CitizenDevice | Locker
                            -- | Organization | PickupStation | ProvisioningCode
  TargetId
  Details                   -- jsonb; action-specific payload (old/new status, locker number, reason, ...)
}
```

## 4. Invariants

### 4.1 Identity & accounts

- `CitizenUser.EMSO` is unique.
- A `CitizenUser` is created only as a result of SI-TRUST onboarding.
- An `EmployeeAccount` belongs to exactly one organization (cannot be reassigned).
- `EmployeeDevice`: at most one row per `EmployeeAccountId` with `RevokedAt IS NULL` (single active device per employee).
- `CitizenDevice`: no cap on simultaneously active devices per citizen.
- `ProvisioningCode` is single-use: once `RedeemedAt` is set, it cannot be redeemed again. It expires at `ExpiresAt`.
- Redemption of a `ProvisioningCode` with `IsReprovisioningOfEmployeeAccountId` set:
  - Binds the new device to the referenced `EmployeeAccount`.
  - Atomically sets `RevokedAt` on the account's previously-active `EmployeeDevice`.
- Redemption of a `ProvisioningCode` without `IsReprovisioningOfEmployeeAccountId`:
  - Creates a new `EmployeeAccount` populated from the code's pre-filled data.
  - Creates `EmployeeAccountRole` rows from the code's `Roles`.
  - Creates `EmployeeAccountStationAccess` rows from the code's `StationAccess`.
- Roles are orthogonal in the data model. `OrganizationAdmin` implicitly grants the powers of `RecordManager` and `Operator` in the authorization layer, but the database stores only roles that have been explicitly assigned.
- The platform's authentication layer never receives or stores PIN or biometric data. Backend stores only device public keys and verifies signed challenges.

### 4.2 Tenancy & infrastructure

- A `PickupStation` has at most one `StationClaim` row with `ReleasedAt IS NULL` (the active claim). The active claim's `OrganizationId` is the station's current operator.
- A `StationClaim` is created together with its `PickupStation` when an `OrganizationAdmin` self-registers a station. The same operation may also release a prior claim only after it has explicitly been released by the previous claimant.
- A `Locker` belongs to exactly one `PickupStation`. `LockerNumber` is unique within a station.
- Locker occupancy is *derived*, not stored: a locker is occupied iff there is a `Placement` referencing it with `EndedAt IS NULL`.
- `IsServiceable = false` lockers cannot be selected for new placements.

### 4.3 Packages

- A `Package` can be created only by an actor with `RecordManager` or `OrganizationAdmin` in the package's organization.
- At creation, `Package.TargetPickupStationId` must reference a station for which the organization currently holds an active `StationClaim`.
- `Package.RecipientCitizenUserId` must reference an existing `CitizenUser`. Recipients must be SI-TRUST onboarded before a package can be created for them.
- `Package.DeadlineAt` is set when a `Placement` opens on that package, computed as `Placement.OpenedAt + Organization.DefaultPickupDuration` (resolved from `Package.OrganizationId`).
- When a `Placement` ends with `EndReason = RemovedByEmployee`, `Package.DeadlineAt` is cleared to null. A re-insertion opens a new `Placement` and computes a fresh `DeadlineAt`.
- `Package.FinalizedAt` is set when `Status` transitions to `PickedUp` or `Cancelled` (the two terminal states).

### 4.4 Placements

- An *open* `Placement` (`EndedAt IS NULL`) exists only while its package's `Status` is `InLocker` or `NotPickedUp`. Closed placements (`EndedAt` set) persist as immutable historical records for the lifetime of the package.
- Per-package invariant: at most one `Placement` row per `PackageId` with `EndedAt IS NULL` (a package is in at most one locker at a time).
- Per-locker invariant: at most one `Placement` row per `LockerId` with `EndedAt IS NULL` (a locker holds at most one package at a time).
- A `Placement` is opened only by an actor with `Operator` or `OrganizationAdmin` in the package's organization, and only at a locker whose station equals `Package.TargetPickupStationId`, and only if that locker is `IsServiceable = true`.
- A `Placement` is never reopened. Each insertion is its own row. A `Placement` with `EndedAt IS NULL` is the only mutable state on the entity; once `EndedAt` is set, the row is immutable.
- `EndReason` and the corresponding "ended by" FK are set atomically with `EndedAt`. The valid pairings are:
  - `PickedUpByCitizen` → `EndedByCitizenUserId` set, `EndedByEmployeeAccountId` null.
  - `RemovedByEmployee` → `EndedByEmployeeAccountId` set, `EndedByCitizenUserId` null.
  - `RetrievedAfterExpiry` → `EndedByEmployeeAccountId` set, `EndedByCitizenUserId` null.

### 4.5 Delegations

- `Delegation.DelegatorCitizenUserId` must equal `Package.RecipientCitizenUserId` for the referenced package.
- `Delegation.DelegateCitizenUserId` must reference an existing `CitizenUser` (delegates must be SI-TRUST onboarded).
- A delegation is *usable* iff `RevokedAt IS NULL` AND its package's current `Status` is `InLocker`.
- A delegation can be revoked by its delegator at any time (`RevokedAt` set).
- Multiple delegations may exist per package, including to different delegates; first valid pickup wins.

### 4.6 Audit

- Every state-changing action emits exactly one `AuditLogEntry`.
- Every physical locker-opening API call emits one `LockerOpened` entry, independently of any higher-level business action that triggered it.
- The audit table is append-only: application database users have INSERT and SELECT permissions only. UPDATE and DELETE are revoked at the database level.
- For `ActorKind != System`, exactly one of `ActorCitizenUserId`, `ActorEmployeeAccountId`, `ActorSystemAdminId` is non-null; the other two are null. For `ActorKind = System`, all three are null.

## 5. Package state machine

```
                  cancel (RecordManager / OrgAdmin)
AwaitingPlacement ─────────────────────────────────────► Cancelled  (terminal)
        │
        │ Operator inserts:
        │   - opens Placement
        │   - DeadlineAt = OpenedAt + org.DefaultPickupDuration
        ▼
   InLocker
   ├── citizen picks up
   │     -> Placement.EndReason = PickedUpByCitizen
   │     -> Status = PickedUp (terminal)
   │
   ├── Operator removes
   │     -> Placement.EndReason = RemovedByEmployee
   │     -> DeadlineAt cleared
   │     -> Status = AwaitingPlacement
   │
   └── DeadlineAt passes while still in locker
         -> Status = NotPickedUp
            (Placement is still open at this point; the locker remains occupied
             until an Operator retrieves it)
            │
            │ Operator retrieves:
            │   -> Placement.EndReason = RetrievedAfterExpiry
            │   -> Status = AwaitingPersonalPickup
            ▼
       AwaitingPersonalPickup
         ├── RecordManager / OrgAdmin marks picked up (web UI, manual)
         │     -> Status = PickedUp (terminal)
         │
         └── RecordManager / OrgAdmin cancels
               -> Status = Cancelled (terminal)
```

Cancellation is permitted from `AwaitingPlacement`, `InLocker` (effectively: removal + cancel are two distinct operations the actor must perform in order), and `AwaitingPersonalPickup`. It is not permitted from `PickedUp` (terminal) or from `NotPickedUp` directly (the operator must first retrieve, transitioning to `AwaitingPersonalPickup`, at which point cancellation becomes valid).

Pure direct transitions:

| From | To | Triggered by | Side effects |
|------|-----|--------------|--------------|
| AwaitingPlacement | InLocker | Operator inserts | New Placement opened; DeadlineAt computed |
| InLocker | PickedUp | Citizen picks up | Placement ended (PickedUpByCitizen); FinalizedAt set |
| InLocker | AwaitingPlacement | Operator removes | Placement ended (RemovedByEmployee); DeadlineAt cleared |
| InLocker | NotPickedUp | Time (DeadlineAt) | None (placement remains open) |
| NotPickedUp | AwaitingPersonalPickup | Operator retrieves | Placement ended (RetrievedAfterExpiry) |
| AwaitingPersonalPickup | PickedUp | RecordManager web action | FinalizedAt set |
| AwaitingPlacement | Cancelled | RecordManager web action | FinalizedAt set |
| AwaitingPersonalPickup | Cancelled | RecordManager web action | FinalizedAt set |

## 6. Audit action catalog

Stored as strings. Initial set, extensible:

**Packages & placements**
- `PackageCreated`
- `PackagePlaced`
- `PackagePickedUpByCitizen`
- `PackageRemovedByEmployee`
- `PackageExpired`              — deadline passed (emitted by the scheduler)
- `PackageRetrievedAfterExpiry`
- `PackageMarkedPickedUpManually`
- `PackageCancelled`

**Delegations**
- `DelegationCreated`
- `DelegationRevoked`
- `DelegationUsedAtPickup`      — recorded alongside `PackagePickedUpByCitizen` when pickup occurred via delegation

**Employees, devices, codes**
- `ProvisioningCodeIssued`
- `ProvisioningCodeRedeemed`
- `EmployeeAccountCreated`
- `EmployeeAccountDisabled`
- `EmployeeAccountReenabled`
- `EmployeeAccountRoleGranted`
- `EmployeeAccountRoleRevoked`
- `EmployeeStationAccessGranted`
- `EmployeeStationAccessRevoked`
- `EmployeeDeviceRegistered`
- `EmployeeDeviceRevoked`
- `CitizenDeviceRegistered`
- `CitizenDeviceRevoked`

**Citizens**
- `CitizenOnboarded`

**Tenancy & infrastructure**
- `OrganizationCreated`         — by SystemAdmin
- `StationClaimed`              — org self-registers / claims a station
- `StationReleased`
- `LockerCreated`
- `LockerServiceabilityChanged`
- `LockerOpened`                — physical hardware open call; emitted every time

`Details` (jsonb) carries the action-specific context (previous status, new status, locker number, deadline timestamp, reason, etc.).

## 7. Authentication model

Authentication mechanics are referenced here only to the extent that they shape the domain.

- **Citizen.** SI-TRUST onboarding establishes the `CitizenUser`. Each device thereafter generates a keypair locally (private key in secure storage) and registers the public key with the backend, authenticated by a fresh SI-TRUST session. Day-to-day authentication uses device-signed challenges unlocked by PIN/biometric on the device. Multiple active devices per citizen are allowed and self-managed.
- **Employee.** An `OrganizationAdmin` issues a `ProvisioningCode` populated with the new employee's basic data and intended roles/station access. The employee enters the code in the app; the device generates a keypair and registers its public key with the backend. The account is bound to that device. Day-to-day authentication uses device-signed challenges unlocked by PIN/biometric on the device. Re-binding to a new device is performed by issuing a fresh `ProvisioningCode` with `IsReprovisioningOfEmployeeAccountId` set to the existing account; redemption auto-revokes the prior device.
- **System admin.** Separate aggregate with its own authentication path. Out of scope for this spec.

PINs and biometric data never leave the user's device.

## 8. Authorization

Authority scopes:

- **Platform scope (System admin):** create `Organization`, create `PickupStation` and its `Locker` rows.
- **Org scope:**
  - `OrganizationAdmin`: manage employees (issue and revoke provisioning codes, grant/revoke roles, grant/revoke station access, disable accounts), manage station claims for the org, edit organization settings (e.g. `DefaultPickupDuration`). Implicitly inherits `RecordManager` and `Operator` powers within the org.
  - `RecordManager`: create packages, cancel packages from any state where cancellation is allowed, mark `AwaitingPersonalPickup → PickedUp` manually, create delegations on behalf of recipients only when explicitly authorized (out of scope here; default: delegations are created by citizens themselves).
  - `Operator`: open, end placements (insert, remove, retrieve). Visibility scoped to packages whose `TargetPickupStationId` is in the operator's `EmployeeAccountStationAccess` set.

Visibility rule for `RecordManager` and `Operator`: they only see `Package` rows whose `TargetPickupStationId` is in their station access set. `OrganizationAdmin` sees all packages of the org regardless of station.

Insertion rule (v1): an `Operator` may open a `Placement` only at a locker whose `PickupStationId` equals the package's `TargetPickupStationId`.

## 9. Citizen pickup flow

1. Citizen scans a station QR code in the mobile app. The QR encodes the `PickupStationId`.
2. The app sends an authenticated request: "list packages for me at station X". The backend resolves the actor from the device-signed session.
3. Backend returns packages where:
   - `Placement.EndedAt IS NULL`, AND
   - `Placement.LockerId.PickupStationId = X`, AND
   - either `Package.RecipientCitizenUserId = me`, OR a usable `Delegation` exists with `DelegateCitizenUserId = me` and `PackageId = Package.Id`.
4. Citizen selects a package; backend opens the corresponding locker (emits `LockerOpened` audit) and atomically ends the `Placement` with `EndReason = PickedUpByCitizen`, sets `Package.Status = PickedUp`, sets `Package.FinalizedAt`, emits `PackagePickedUpByCitizen` (and `DelegationUsedAtPickup` if applicable).

## 10. Out of scope

The following are explicitly deferred:

- **Document sub-entities within a package.** A `Package` is one logical unit; its real-world content (one or many documents/items) is not modeled.
- **Organization branches / hierarchical locations.** Station access lists serve the v1 need. A future `OrganizationBranch` aggregate may be introduced if HR-style grouping or regional roles emerge.
- **Shared (multi-org) pickup stations.** Schema is shaped for this (`StationClaim`) but v1 enforces exclusive active claims.
- **Standing delegations** ("X may pick up any package addressed to me at org Y"). v1 supports per-package delegations only; a `StandingDelegation` aggregate can be added later without breaking existing tables.
- **System-admin authentication mechanics.**
- **Hardware integration details** (token acquisition, retry semantics, offline behavior). The domain only states that locker opens are audited and that a placement is the durable record of "physical contents are in this locker".
- **Notifications** (email/SMS/push to citizens on package availability, deadline reminders, etc.).
- **Standing organization policies beyond `DefaultPickupDuration`.**

## 11. Future-proofing notes

- `StationClaim` makes the move to shared stations a data-only change.
- `EmployeeAccountStationAccess` makes the move to `OrganizationBranch` a backfill: derive initial branches from current access groupings, then re-point `EmployeeAccount.BranchId` and `PickupStation.BranchId`.
- The audit log's string-typed `Action` and jsonb `Details` allow new event types without migrations.
- Splitting `Package` from `Placement` allows future placements to add concerns (e.g. tamper events, sensor readings) without touching `Package`.