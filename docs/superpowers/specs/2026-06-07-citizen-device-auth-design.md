# Sub-project A — Device Onboarding & Challenge/Signature Login

**Date:** 2026-06-07
**Status:** Approved design, ready for implementation planning
**Scope:** Backend (`backend/`) + Mobile (`ePrevzemMobile/`)

## Context

`ePrevzemMobile` is a citizen-facing pickup client whose data layer is currently almost
entirely fake/in-memory. We are replacing the fakes with real backend integration. A gap
analysis showed the backend's HTTP surface is today almost entirely organization / employee /
admin-facing — there is **no citizen-facing API** and **no device challenge/signature
authentication**, even though the domain (`CitizenUser`, `CitizenDevice`, `EmployeeAccount`,
`EmployeeDevice`, `CitizenActivationCode`, `ProvisioningCode`, `RefreshToken`) is already in place.

The full citizen API was decomposed into four sub-projects:

- **A. Device onboarding & auth** (this spec) — foundation; everything else needs citizen JWTs.
- B. Citizen "my pickups".
- C. Citizen "my activity log".
- D. Citizen delegations (new domain).

This spec covers **A only**. B/C/D get their own spec → plan → build cycles afterward.

### Decisions taken during brainstorming

- Build the citizen API on the backend first, then point mobile at a **live local backend**.
- Authentication uses the **device challenge/signature** model (not email/password) for the
  mobile client, issuing **access + refresh tokens** to match the existing org/admin flows.
- Sub-project A covers **both** citizen activation codes **and** the employee provisioning
  redeem (the current `501` stub), because the mobile onboarding UI handles both through one
  code entry.
- Challenge handling is **stateful**: a one-time-use, short-lived `DeviceChallenge` per device.

## Goals

1. A device (citizen or employee) can onboard from a code, registering its public key.
2. A registered device can authenticate via challenge → signature → access+refresh tokens.
3. The mobile `RegistrationRepository` and `AuthRepository` are backed by real HTTP calls.
4. Replay-safe, standards-based crypto that matches what the mobile already produces.

## Non-goals

- Citizen pickups, activity log, delegations (sub-projects B/C/D).
- Changing the org/admin password login flows.
- Biometric/PIN local crypto (already implemented on-device in `LocalSecurityRepository`).

## Crypto protocol (shared by citizen & employee)

Dictated by the existing mobile implementation (`SecurityCrypto.android.kt` /
`.ios.kt`), not invented here:

- **Keys:** EC P-256 / `secp256r1`. The device generates the pair.
- **Public key on the wire:** X.509 SubjectPublicKeyInfo, **PEM**-encoded string.
- **Signature:** `SHA256withECDSA`, DER (`Rfc3279DerSequence`), base64-encoded.
- **Challenge:** 32 random bytes, base64-encoded.
- **Backend verification:**
  `ECDsa.ImportSubjectPublicKeyInfo(der)` then
  `VerifyData(challenge, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)`.
  The PEM is stripped to DER before import; `CitizenDevice.PublicKey` / `EmployeeDevice.PublicKey`
  store the DER bytes (current `byte[]` columns).

A backend crypto round-trip test (generate P-256 in-test, sign, verify) locks this wire format
against the mobile.

## Endpoint surface — unified onboarding

The mobile enters **one code** and gets back either a citizen (`RegularUser`) or an `Employee`.
The backend exposes a unified onboarding pair that internally dispatches to the
citizen-activation or employee-provisioning machinery, so the client does not have to guess the
code type.

### `GET api/onboarding/{code}` — anonymous
Peek. Maps mobile `validateCode` + `fetchAccountPreview`.
Looks up `code` in **both** `CitizenActivationCode` and `ProvisioningCode`.
- 200 → `{ kind: "Citizen" | "Employee", firstName, lastName, email?, organizationName?, roles?, expiresAt }`
- 404 if neither matches.
- 410 if found but expired or already redeemed.

### `POST api/onboarding/{code}/redeem` — anonymous
Maps mobile `confirmAccount(code, publicKey)`.
Body: `{ publicKeyPem, deviceFingerprint, label? }`.
- **Citizen branch:** `CitizenActivationCode.Redeem(now)` + `CitizenUser.RegisterDevice(...)`.
- **Employee branch:** `ProvisioningCode.Redeem(now, employeeAccountId)` resolving-or-creating
  the `EmployeeAccount` (resolve when the code is bound to an existing account, e.g. the
  `AddEmployeeMember` path or `IsReprovisioningOfEmployeeAccountId`; otherwise create from the
  code's `PreFilledInfo` + roles), then `EmployeeAccount.RegisterDevice(...)`
  (auto-revokes any prior active device).
- Issues access + refresh tokens.
- 200 → `{ role, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, deviceId, firstName, lastName, organizationId?, organizationName? }`.

### `POST api/auth/device/challenge` — anonymous
Maps mobile `getChallenge(deviceId)`.
Body: `{ deviceId }`. Persists a one-time `DeviceChallenge`. Returns `{ challenge, expiresAt }`.
(Challenge TTL ~2 minutes.)

### `POST api/auth/device/verify` — anonymous
Maps mobile `verifySignature(deviceId, signature)`.
Body: `{ deviceId, signature }`. Consumes the challenge (must be unconsumed + unexpired),
verifies the signature against the device's stored public key (citizen **or** employee),
issues access + refresh.
Returns the same token + profile shape as redeem.

### `POST api/auth/device/refresh` — anonymous
Token renewal for device sessions (citizen + employee). Body: `{ refreshToken }`. Rotates the
refresh token (existing `RefreshToken.Rotate`) and returns a new pair.

### Existing org-provisioning routes
`GET api/org/provisioning/{code}` (peek) and `POST api/org/provisioning/{code}/redeem` (today
`501`) are **kept working** to avoid breaking the React/Flutter mocks: the redeem is rewritten
to delegate to the unified redeem handler; the peek remains (delegating to the unified peek).

## Backend changes by layer

Strict dependency flow preserved: `Api → Application → Domain`, `Infrastructure → Application, Domain`.

### Domain
- `RefreshToken`: add `CitizenUserId?` property + `IssueForCitizen(...)` factory; include
  citizen in `Rotate` / `RecordChainRevocation` events.
- New `DeviceChallenge` aggregate: `{ Id, DeviceId (Guid), DeviceKind (Citizen|Employee),
  Nonce (byte[]), ExpiresAt, ConsumedAt? }` with `Issue(...)` and `Consume(now)` (guards
  double-consume and expiry).

### Application
- New ports: `ISignatureVerifier`, `IDeviceChallengeRepository`, device-by-id lookups
  (`ICitizenDeviceRepository` / `IEmployeeDeviceRepository`, or extend existing repositories),
  and `ITokenService.IssueAccessToken(CitizenUser)`.
- Use cases (MediatR): `PeekOnboardingCodeQuery`, `RedeemOnboardingCodeCommand` (dispatches
  citizen vs employee), `IssueDeviceChallengeCommand`, `VerifyDeviceSignatureCommand`,
  `RefreshDeviceTokenCommand`. FluentValidation validators for each.
- Emit existing domain events (`CitizenDeviceRegistered`, `EmployeeDeviceRegistered`, login
  events) so the append-only audit log records onboarding & device logins (feeds sub-project C).

### Infrastructure
- `SignatureVerifier` (ECDsa P-256, SHA256, DER).
- `DeviceChallengeRepository` + EF configuration + migration.
- `JwtTokenService` citizen overload: role claim `Citizen`, `sub` = citizen user id,
  `deviceId` claim for device-bound sessions.
- Device lookup queries (find `CitizenDevice` / `EmployeeDevice` by id, with public key).
- `RefreshToken` citizen FK: EF configuration + migration.

### API
- New `OnboardingController` and `DeviceAuthController`.
- Rewrite `OrgProvisioningController.Redeem` to delegate to the unified handler.
- Confirm JWT bearer auth accepts a `Citizen` role (consumed by B/C/D via
  `[Authorize(Roles = "Citizen")]`).

## Mobile changes

Compose Multiplatform design-system rules unaffected (this is data layer only).

- `PlatformConfig`: add `eprevzemApiBaseUrl` (live local backend URL).
- Shared Ktor `ApiClient` (reuse the `Direct4MeLockerRepository` Ktor setup pattern): JSON
  content negotiation, bearer-token injection, refresh-on-401.
- `HttpRegistrationRepository` implementing `validateCode` / `fetchAccountPreview` /
  `confirmAccount` against the onboarding endpoints; map `kind` → `RegularUser` / `Employee`.
- `HttpAuthRepository` implementing `getChallenge` / `verifySignature` against the device
  endpoints; persist the `deviceId` returned at redeem for later challenge calls.
- Token persistence in `SecureStorage` (access + refresh); per-platform `deviceFingerprint`
  source.
- `AppContainer`: swap `FakeRegistrationRepository` → `HttpRegistrationRepository`,
  `FakeAuthRepository` → `HttpAuthRepository`. Keep the fakes available for tests.

## Testing

- **Backend** (xUnit + Testcontainers Postgres + `WebApplicationFactory`, never mock the DB):
  - Redeem happy path — citizen and employee (resolve and create variants).
  - Challenge → verify happy path.
  - Replay rejection: consumed challenge, expired challenge.
  - Bad-signature rejection.
  - Refresh rotation (and rejection of a rotated/expired refresh token).
  - Domain unit tests: `RefreshToken.IssueForCitizen`, `DeviceChallenge` guards.
  - Crypto round-trip test (P-256 sign in-test → `SignatureVerifier`) locking the wire format.
- **Mobile:**
  - Repository tests via Ktor `MockEngine` against the agreed contract.
  - A live smoke test against the local backend.

## Open items (resolve in the plan, non-blocking)

- **Employee redeem resolve-vs-create.** `AddEmployeeMember` pre-creates the account + password
  + code; `IssueProvisioningCode` is standalone. Redeem resolves an existing account when the
  code is bound to one, else creates from `PreFilledInfo` + roles. Verify both paths against the
  data when writing the plan.
- **Keep vs deprecate** the standalone `GET api/org/provisioning/{code}` peek. Leaning: keep,
  delegating to the unified peek.

## Sequencing

1. Backend domain + migrations (`RefreshToken` citizen FK, `DeviceChallenge`).
2. Backend application use cases + ports.
3. Backend infrastructure (verifier, repos, token service, EF config).
4. Backend API controllers + delegation rewrite; integration tests green.
5. Mobile Ktor client + token storage.
6. Mobile `HttpRegistrationRepository` + `HttpAuthRepository`; wire `AppContainer`.
7. Live end-to-end smoke test against the local backend.
</content>
</invoke>
