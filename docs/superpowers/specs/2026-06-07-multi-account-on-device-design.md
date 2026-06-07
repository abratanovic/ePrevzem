# Multiple accounts on one device — Design

**Date:** 2026-06-07
**Subproject:** `ePrevzemMobile/` (Kotlin Multiplatform / Compose Multiplatform)
**Status:** Approved design — ready for implementation planning

## Problem

One person may use the same phone for more than one ePrevzem identity — for
example an **employee** account and a personal **citizen** account. Today the app
supports a single registered identity: a second device registration overwrites
the first one's credentials. We want the device to hold multiple accounts, each
registered the same way (entering a code), and — when two or more accounts are
saved — to show an account chooser **before** the biometric/PIN unlock so the
user picks which identity to sign in with.

## Goals

- Register and persist multiple accounts on one device, each via the existing
  code-entry registration flow.
- Each account has its **own** credentials: keypair, PIN, biometric enrollment,
  `deviceId`, and tokens. Registering or resetting one account never affects
  another.
- On launch (when unauthenticated):
  - **0 accounts** → Welcome / registration.
  - **1 account** → go straight to that account's biometric/PIN unlock.
  - **2+ accounts** → show an account chooser first; selecting a row leads to
    that account's unlock.
- The chooser shows, per account: **name & surname**, **account type**
  (Zaposleni / Občan), and **organization name** for employees.
- An "Add account" action from the chooser starts the registration flow again.

## Non-goals (this version)

- Removing / managing saved accounts from the chooser (deferred — `removeProfile`
  already exists in the data layer for a later iteration).
- Any backend changes. The backend already issues an independent device
  registration (`deviceId` + tokens) per redeemed code; the mobile app simply
  stops discarding the previous one.
- Simultaneous multi-session (being logged into two accounts at once). Exactly
  one account is the active authenticated session at a time.

## Current state (what already exists)

The data layer is **already multi-account aware**:

- `domain/AppUser` is a sealed type: `RegularUser` (citizen) and `Employee`
  (with `organizationName`, `organizationType`, `organizationLocation`,
  `roles`). The chooser's display fields map directly onto this.
- `data/auth/SessionStore` + `PersistedSessionStore` persist a
  `List<AppUser>` in secure storage and expose `profiles`, `addProfile`,
  `switchProfile`, `removeProfile`, `setAuthenticated`, `activeProfile`,
  `clear`, `forgetAllIdentities`, and `hydrate` (with legacy single-user
  migration).
- The account **id is the backend `deviceId`** — `confirmAccount` returns a
  `DeviceSessionDto` whose `toAppUser()` sets `id = deviceId`. This id is the
  namespace key for all per-account credentials.

What is **device-global today and must become per-account**:

- `data/security/LocalSecurityRepository` — one keypair / PIN salt / encrypted
  private key / biometric AES key under fixed keys
  (`security.public_key_pem`, …). `register()` overwrites them.
- `data/auth/DeviceSessionStore` — one `deviceId` + access/refresh/expires under
  fixed keys (`auth.device_id`, …). `saveSession()` overwrites them. The
  `auth.device_fingerprint` is genuinely device-global and stays shared.
- `data/security/HttpAuthRepository` — `getChallenge` / `verifySignature`
  read the single `deviceId` + tokens.

What is **missing**:

- No account chooser screen.
- `App.kt` routes unauthenticated users straight to a single `LoginRoute`
  (biometric/PIN) → `activeProfile()` → home. No per-account selection.

## Architecture

The namespace key for every per-account credential is the account id
(= backend `deviceId`). Credential storage keys move from a fixed string to an
account-scoped one, e.g. `security.public_key_pem` →
`security.<accountId>.public_key_pem` and `auth.device_id` →
`auth.<accountId>.device_id`.

### 1. Per-account credential storage

- **`SecurityRepository`** — methods become account-scoped (take an
  `accountId`, or are obtained from an account-scoped factory). All storage
  keys are namespaced by account id. `isRegistered(accountId)`,
  `isBiometricEnabled(accountId)`, `signChallengeWithPin(accountId, pin,
  challenge)`, `signChallengeWithBiometric(accountId, challenge)`,
  `enableBiometric` / `disableBiometric` / `changePin` / `reset` all scoped.
- **`DeviceSessionStore`** — token/`deviceId` keys namespaced by account id.
  `saveSession` already receives `deviceId`, so it writes `auth.<id>.*`
  directly. The physical-device `fingerprint` stays under a single global key
  (shared across accounts — it identifies the phone).
- **`HttpAuthRepository`** — `getChallenge` / `verifySignature` operate on the
  selected account's `deviceId` + tokens.

### 2. Registration: staging → commit

The keypair is generated inside `register()` **before** the redeem response
returns the real `deviceId` (the preview temporarily exposes `id = code`; the
authoritative id arrives only with `DeviceSessionDto`). So registration is a
two-phase write:

1. `register(pin, biometric)` writes the new credentials into a **staging**
   namespace and returns the public key (as today).
2. `confirmAccount(code, publicKey)` returns the user carrying the real
   `deviceId`, and `saveSession` writes that account's tokens under
   `auth.<deviceId>.*`.
3. A new **commit** step promotes the staging credentials to
   `security.<deviceId>.*`.
4. `addProfile(user)` + `setAuthenticated(user.id)` as today.

On any failure, **only** the staging namespace is wiped; existing accounts are
untouched. This replaces today's global-overwrite-then-`forgetAllIdentities`
failure path, which would currently destroy other accounts.

### 3. Account chooser — new feature `feature/accountpicker/`

Follows the state + event split (stateless `Screen`, sealed `Event`, `State`,
stateful `Route`), design-system components only (`E*`), tokens-only styling,
Slovenian UI text. Each row renders:

- **Name & surname** — `AppUser.fullName`.
- **Account type chip** — "Zaposleni" for `Employee`, "Občan" for
  `RegularUser`, via `EStatusChip`.
- **Organization name** — `Employee.organizationName` (employee rows only).

Plus an **"Dodaj račun"** (Add account) action.

Events: `AccountSelected(accountId)`, `AddAccountClicked`. State: the list of
accounts (projected from `sessionStore.profiles`).

### 4. Routing changes (`App.kt`)

- New destinations: `AccountPicker`, and account-scoped `Login(accountId)`.
- After `hydrate()`, on `Unauthenticated`, decide by `profiles`:
  - empty → `Welcome`
  - exactly one → `Login(profiles[0].id)` (skip chooser)
  - two or more → `AccountPicker`
- Chooser row tap → `Login(accountId)`; "Add account" → `RegistrationCode`.
- `LoginRoute(accountId)` signs that account's challenge with that account's
  key + `deviceId`, then `setAuthenticated(accountId)` → home by user type
  (existing `RegularUser` → ActivePickups, `Employee` → OperatorHome).
- "Reset secure storage" stays, scoped so it resets the relevant account's
  credentials rather than wiping all identities; from there route back to the
  chooser (if other accounts remain) or Welcome (if none).
- The existing background-lock auto-`clear()` behavior is preserved; on
  re-entry the routing rules above apply (so a 2+-account user returns to the
  chooser).

## Error handling & edge cases

- Re-registering an existing account id → `addProfile` dedups by id; credentials
  are refreshed via the staging→commit path.
- Failed registration never touches other accounts (staging isolation).
- Corrupt/missing credentials for one account surface as that account's login
  failure only — not a global wipe.
- Single remaining account after a (future) removal → next launch skips the
  chooser per the routing rules.

## Testing

- **`SecurityRepository`** — two accounts coexist; registering, resetting, or
  changing the PIN of one leaves the other intact; staging→commit promotes
  keys to the correct namespace; failed commit wipes only staging.
- **`DeviceSessionStore`** — per-account `deviceId`/token isolation; shared
  fingerprint stays stable across accounts.
- **`PersistedSessionStore`** — switching between two accounts updates the
  active session correctly (extend existing coverage if needed).
- **Account chooser** — renders citizen vs employee rows correctly (type chip,
  org name shown only for employees); emits `AccountSelected` / `AddAccountClicked`.
- **Routing** — 0 / 1 / 2-account launch decisions resolve to
  Welcome / Login / AccountPicker respectively.

## Build / verify

From `ePrevzemMobile/` (use `gradlew.bat` on Windows):

```
./gradlew :composeApp:compileCommonMainKotlinMetadata   # fast compile check
./gradlew :composeApp:allTests
```
