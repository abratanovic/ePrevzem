# Design: Backend-mediated locker open + employee package insertion

Status: approved decisions, pending implementation approval
Date: 2026-06-07
Branch: PRVZM-38

## Goal

Two related flows, both routing the smart-locker hardware (Direct4Me) through
**our** backend instead of the mobile app talking to Direct4Me directly:

- **Flow A — Citizen pickup open.** The citizen taps "Odkleni predalček",
  verifies identity locally, the backend opens the correct box and returns the
  audio token, the phone plays it, and the pickup is committed only when the
  user taps "Končaj".
- **Flow B — Employee insertion.** An Operator scans a pickup-station QR, sees
  the packages awaiting placement *for that station*, picks one and a free
  locker, the backend opens that locker, the operator inserts the package and
  taps "closed", and the placement is persisted (package → InLocker, deadline
  set).

## Confirmed decisions

1. **Box identity:** add a `BoxId` (Direct4Me numeric box id, `long`) to the
   `Locker` aggregate. **Required at locker creation.** The mobile app no longer
   knows or scans the box id for pickup — the backend resolves `BoxId` from the
   package's locker. Existing rows backfilled with a placeholder in the migration.
2. **Citizen pickup commit:** open endpoint returns the token only; a **separate
   confirm** endpoint marks `PickedUp`. "Predalček se ni odprl" does **not** commit.
3. **Employee insert commit:** **no reservation.** Open returns the token;
   `Place` (InLocker + deadline) persists only on the "closed" confirm. Small
   race window accepted; confirm re-validates the locker is still free.
4. **Insert authorization:** Operator role only (`EmployeeAccount.CanOperateLockers`).
5. **Pending list scope:** only packages whose target station == the scanned station.
6. **Station QR:** encodes the station **serial number** (`GetBySerialNumberAsync`).
7. **Direct4Me credentials move server-side.** The mobile `direct4MeApiKey` /
   Direct4Me base URL are removed from the app; the key lives in backend config.

## Current state (verified)

- Mobile opens the box directly: scans box QR → `boxId: Long` →
  `POST {direct4me}/Access/openbox` (Bearer key) → base64/gzip **WAV** → plays it.
  `Direct4MeLockerRepository` + `TokenAudioPlayer`. `UnlockRoute` drives it.
- Backend has **no** Direct4Me integration (only a commented `ILockerGateway`
  placeholder in `DependencyInjection.cs`).
- Domain is ready: `Package.Place(placementId, lockerId, employeeId, pickupDuration, now)`,
  `Package.PickUpByCitizen(citizenId, now)`, `Organization.DefaultPickupDuration`,
  `PickupStation.AddLocker`, `Locker`, `StationClaim` (active-claim model).
- `CreatePickupCommand` is the authz/claim template (active station claim for the
  org; role/permission/active checks).

---

## Backend changes

### 1. Domain — `Locker.BoxId`

- `Locker`: add `public long BoxId { get; private set; }`. `Locker.Create(id,
  stationId, lockerNumber, boxId)` validates `boxId > 0`.
- `PickupStation.AddLocker(LockerId id, int lockerNumber, long boxId, DateTimeOffset? now)`
  — thread `boxId` through; keep the duplicate-locker-number guard.
- `LockerConfiguration` (EF): map `box_id bigint not null`.
- **Migration** `Lockers_AddBoxId`: add `box_id bigint not null default 0`
  (backfill placeholder for existing rows), then drop the default so future
  inserts must supply it. New lockers always set it explicitly.

### 2. Station registration carries box ids

- `RegisterPickupStationCommand`: replace `IReadOnlyList<int> LockerNumbers`
  with `IReadOnlyList<LockerRegistration> Lockers` where
  `LockerRegistration(int Number, long BoxId)`. Handler calls
  `station.AddLocker(LockerId.New(), l.Number, l.BoxId, now)`.
- Update `RegisterPickupStationValidator`, the API request DTO, and the
  `RegisterPickupStationHandlerTests` / `PickupStationTests` accordingly.

### 3. Locker gateway port + Direct4Me adapter

- **Port** `ILockerGateway` (Application/Common/Abstractions):
  `Task<byte[]> OpenBoxAsync(long boxId, CancellationToken ct = default)` —
  returns the ready-to-play WAV bytes; throws `LockerOpenException` on
  API/network failure (carries the Direct4Me error number when present).
- **Adapter** `Direct4MeLockerGateway` (Infrastructure/Lockers): typed
  `HttpClient`; `POST {BaseUrl}/Access/openbox` with `{ boxId, tokenFormat }`,
  Bearer key; base64-decode + gunzip the `data` field → WAV bytes (the decode
  logic currently in the mobile repo moves here).
- **Config** `Direct4MeOptions { BaseUrl, ApiKey, TokenFormat = 1 }` bound from
  configuration; key supplied via user-secrets / env (never committed).
- Register the typed client + options in `DependencyInjection.cs` (replace the
  commented placeholder).

### 4. Read-repository additions (`IPickupReadRepository`)

- `Task<long?> GetActivePickupBoxIdAsync(CitizenUserId citizenId, PackageId packageId, CancellationToken)`
  — BoxId of the **open** placement's locker for a package the citizen owns and
  that is `InLocker`; null if not found / not theirs / not in a locker.
- `Task<InsertionContextResponse?> GetInsertionContextAsync(OrganizationId orgId, string serialNumber, CancellationToken)`
  — resolve station by serial + active org claim; return station id/serial/location,
  the `AwaitingPlacement` packages targeted at this station, and the **free**
  lockers (serviceable, no open placement). Null if the station doesn't exist or
  the org has no active claim.
- `Task<long?> GetFreeLockerBoxIdAsync(PickupStationId stationId, LockerId lockerId, CancellationToken)`
  — BoxId of a locker that belongs to the station, is serviceable, and has no
  open placement; null otherwise (used by employee open + re-checked on confirm).

New DTOs (`Application/Pickups/Dtos`): `InsertionContextResponse`,
`InsertionPackageResponse(Guid Id, string Reference, string Description, string RecipientName)`,
`FreeLockerResponse(Guid LockerId, int LockerNumber)`, and a shared
`LockerTokenResponse(string TokenBase64)` for the two open endpoints.

### 5. Commands / queries

**Flow A (citizen):**
- `OpenCitizenPickupCommand(Guid CitizenUserId, Guid PickupId) : IRequest<LockerTokenResponse>`
  — `GetActivePickupBoxIdAsync` → `ILockerGateway.OpenBoxAsync` →
  `LockerTokenResponse(Convert.ToBase64String(wav))`. No state change.
  Throws `PickupNotFoundException` (not theirs / not InLocker), `LockerOpenException`.
- `ConfirmCitizenPickupCommand(Guid CitizenUserId, Guid PickupId) : IRequest`
  — load `Package` via `IPackageRepository`, verify recipient == citizen and
  status `InLocker`, `package.PickUpByCitizen(citizenId, now)`, save.

**Flow B (employee, Operator):**
- `GetInsertionContextQuery(Guid OrganizationId, string SerialNumber)` →
  `InsertionContextResponse?` (404 → station-not-found / not-claimed).
- `OpenInsertionLockerCommand(Guid OrganizationId, Guid ActorEmployeeId, Guid PackageId, Guid LockerId) : IRequest<LockerTokenResponse>`
  — verify employee active + `CanOperateLockers` + org active claim on the
  locker's station; verify package `AwaitingPlacement` & target station ==
  locker's station; `GetFreeLockerBoxIdAsync` → gateway open → token. No state change.
- `ConfirmInsertionCommand(Guid OrganizationId, Guid ActorEmployeeId, Guid PackageId, Guid LockerId) : IRequest<PickupResponse>`
  — re-validate operator + claim + package state + locker still free; load
  `Organization` for `DefaultPickupDuration`; `package.Place(PlacementId.New(),
  lockerId, employeeId, org.DefaultPickupDuration, now)`; save; return updated `PickupResponse`.

### 6. Controllers

- `CitizenPickupsController` (existing): add
  - `POST /api/citizen/pickups/{pickupId}/open` → `LockerTokenResponse` (200),
    404 not-found, 502 `LockerOpenException`.
  - `POST /api/citizen/pickups/{pickupId}/confirm-pickup` → 204, 404/409.
- New `OrgInsertionController` `[Authorize(Roles="Employee")]`, route
  `api/org/insertion` (operator gate enforced in the handler via
  `CanOperateLockers`, like `CreatePickup`):
  - `GET /api/org/insertion/context?serial={serial}` → `InsertionContextResponse`
    (404 if unknown/unclaimed).
  - `POST /api/org/insertion/{packageId}/open` body `{ lockerId }` → `LockerTokenResponse`,
    403 forbidden, 404, 409 (locker taken/wrong station), 502 open failure.
  - `POST /api/org/insertion/{packageId}/confirm` body `{ lockerId }` → `PickupResponse`,
    same error set (409 if locker no longer free).

---

## Mobile changes

### Flow A — citizen pickup via backend
- `LockerRepository` becomes backend-backed (`HttpLockerRepository`):
  - `suspend fun openForPickup(pickupId: String): OpenBoxResult`
  - (Flow B) `suspend fun openForInsertion(packageId: String, lockerId: String): OpenBoxResult`
  - `OpenBoxResult.Success(wavBytes)` unchanged; map non-2xx → `ApiFailure`,
    exceptions → `NetworkFailure`. Calls go through `ApiClient.authorizedPost`
    (new helper mirroring `authorizedGet`: bearer + 401-refresh-retry).
- Remove `Direct4MeLockerRepository`, `OpenBoxDto`, and `direct4MeApiKey` from
  `PlatformConfig` (and the Android BuildConfig field). `TokenAudioPlayer` stays.
- `PickupRepository`: add `suspend fun confirmPickup(pickupId: String): Result<Unit>`
  (`POST .../confirm-pickup`).
- `UnlockRoute`: drop the QR-scan / camera-permission path for pickup. After
  identity verification the route immediately calls `openForPickup(pickupId)`,
  plays the token, shows "Unlocked". "Končaj" → `pickupRepository.confirmPickup`
  → done; "Predalček se ni odprl" → retry `openForPickup` (no commit).
- `App.kt`: `Unlock` destination no longer needs a box id; keep `pickupId`.

### Flow B — operator insertion (new feature module `feature/operator/insertion`)
- New `OrgInsertionRepository` (HTTP): `getInsertionContext(serial)`,
  `openForInsertion(packageId, lockerId)`, `confirmInsertion(packageId, lockerId)`;
  DTOs mirror the backend (`InsertionContextDto`, `InsertionPackageDto`,
  `FreeLockerDto`).
- Screens (state+event split, E* components, Slovenian copy):
  1. **Scan station** → reuse camera/QR scaffolding from `unlock` to read the
     serial; on success fetch insertion context.
  2. **Insertion context** → list pending packages (target station) + free
     lockers; operator picks one of each → `openForInsertion` → play token →
     "Sem zaprl predalček" confirm → `confirmInsertion` → success.
- Wire entry from `OperatorHomeScreen` (already present); gate visibility on the
  Operator role.

---

## Sequencing (backend-first, then mobile)

**Wave 1 — domain + schema (serial, blocks everything):**
B1 `Locker.BoxId` + `AddLocker` + `LockerConfiguration` + migration.
B2 `RegisterPickupStationCommand`/validator/DTO/tests carry box ids.

**Wave 2 — gateway + reads (parallel after Wave 1):**
B3 `ILockerGateway` + `Direct4MeLockerGateway` + options + DI.
B4 `IPickupReadRepository` additions (citizen box id, insertion context, free-locker box id) + DTOs.

**Wave 3 — commands/queries + controllers (parallel after Wave 2):**
B5 Flow A: open + confirm commands, `CitizenPickupsController` endpoints.
B6 Flow B: insertion context query + open/confirm commands, `OrgInsertionController`.

**Wave 4 — mobile (after backend endpoints exist):**
M1 `ApiClient.authorizedPost`; `HttpLockerRepository`; remove Direct4Me from app/config.
M2 Citizen `UnlockRoute` rewire + `confirmPickup`.
M3 Operator insertion repository + DTOs.
M4 Operator insertion screens + `OperatorHomeScreen` entry.

## Out of scope / notes
- No admin UI to set `BoxId` (seed/SQL out of band beyond the create path).
- `LockerOpenException` surfaces as HTTP 502; the app shows the existing
  open-failure UI and offers retry.
- Pickup-duration policy unchanged (`Organization.DefaultPickupDuration`).
