# Operator Home Screen — Design

**Date:** 2026-05-26
**Subproject:** `ePrevzemMobile` (Compose Multiplatform)
**Status:** Proposed

## Problem

The mobile app currently has a single home screen (`ActivePickupsScreen`) modeled around the **regular user** — "your pending document pickups". The same screen is rendered for both `AppUser.RegularUser` and `AppUser.Employee` (Operator), which is wrong: an Operator's job is the inverse of a regular user's. They put documents *into* lockers on behalf of an organization; they don't pick anything up.

This design defines a dedicated Operator home and the workflows it gates: inserting documents, retracting documents from a locker (operator error), removing expired documents, viewing the audit log of these actions, and switching between multiple provisioning profiles on the same device.

## Scope

In scope:
- Operator-specific home screen (pre-QR-scan)
- Paketnik-scoped screen (post-QR-scan) with three context sections
- Per-document insertion sub-flow (multi-step)
- Take-back / expired-removal sub-flow
- Profile tab with multi-profile dropdown
- Changes to `SessionStore` to hold multiple persisted profiles
- New `PickupStatus` enum and operator-side pickup contracts (fake repositories at first)

Out of scope:
- Real backend wiring for the operator endpoints (fakes only — backend contract is a separate spec)
- Per-profile cryptographic keys (all profiles continue to share the device key in `LocalSecurityRepository`)
- Push notifications when an expired pickup needs attention
- Localization beyond Slovenian UI / English code (per repo convention)

---

## Routing & navigation

`App.kt` already routes by `AppDestination`. The authenticated branch must now diverge by user type:

```kotlin
is AuthSession.Authenticated -> when (session.user) {
    is AppUser.RegularUser -> /* existing ActivePickups flow */
    is AppUser.Employee    -> /* new OperatorHome flow */
}
```

New destinations:

| Destination | Purpose |
|---|---|
| `OperatorHome` | Pre-scan landing |
| `OperatorScan` | Hosts the QR scanner |
| `OperatorPaketnik(paketnikId)` | Paketnik-scoped: three sections (insert / in-locker / expired) |
| `OperatorInsertFlow(paketnikId, pickupIds)` | Stepper through selected docs |
| `OperatorRemoveFlow(paketnikId, pickupId, reason)` | Single take-back or expired-removal |

The Profile tab and Zgodovina tab continue to be tab-state inside `OperatorHome`, not separate destinations (same pattern as the existing `ActiveTab` enum in `ActivePickupsScreen`).

---

## Screen 1 — `OperatorHome` (Prevzemi tab, pre-scan)

A minimal, action-first landing screen. The Operator only acts in the context of a specific paketnik, so a generic list of "your pickups" makes no sense here.

```
┌─────────────────────────────────────┐
│  [organization icon]      [MK]      │  ETopBar (Home variant)
├─────────────────────────────────────┤
│  DOBRODOŠLI                         │
│  Pozdravljeni, Marko                │
│                                     │
│  ┌──────────────────────────────┐   │
│  │   [qr_scan icon, large]      │   │  Primary action — taps into
│  │   Skeniraj QR kodo           │   │  OperatorScan
│  │   na paketniku               │   │
│  └──────────────────────────────┘   │
│                                     │
│  PREGLED                            │
│  ┌─────────────┐  ┌─────────────┐   │  Read-only summary chips,
│  │ Za vstaviti │  │ V paketniku │   │  populated from a lightweight
│  │     12      │  │      7      │   │  `OperatorOverviewRepository.
│  └─────────────┘  └─────────────┘   │   counts()` call.
│  ┌─────────────┐                    │
│  │  Zapadli    │                    │
│  │      3      │                    │
│  └─────────────┘                    │
├─────────────────────────────────────┤
│   Prevzemi    Zgodovina    Profil   │  EBottomNavigationBar
└─────────────────────────────────────┘
```

**Decisions:**
- One primary CTA. No list of pickups outside paketnik context — they aren't actionable.
- Summary chips are **read-only**. Future enhancement: tapping them deep-links to filtered Zgodovina.
- Bottom-nav structure stays the same as today (`Prevzemi / Zgodovina / Profil`).

---

## Screen 2 — `OperatorPaketnik(paketnikId)`

The QR scan returns a `paketnikId`. The screen loads a single `OperatorPaketnikContext` and renders up to three sections, omitting any that are empty:

```
┌─────────────────────────────────────┐
│  ←  Paketnik #12                    │  ETopBar (Back)
│     Kongresni trg, Ljubljana        │
├─────────────────────────────────────┤
│  ZA VSTAVITEV  (3)                  │  EDetailsSectionLabel
│  ☐  Marko Horvat                    │  EPickupCard variant with
│     Diploma · DOK-2026-0451         │  trailing checkbox
│  ☐  Ana Novak                       │
│     Izpis · DOK-2026-0452           │
│  ☐  Jure Kos                        │
│     Sklep · DOK-2026-0453           │
│                                     │
│  [Vstavi izbrane (0)]               │  EPrimaryButton, disabled
│                                     │  until ≥1 selected
│  ───────────────────────────────    │
│  PREVZEMI V PAKETOMATU  (2)         │
│  Marko Horvat                  →    │  EPickupCard (chevron)
│  Diploma · DOK-2026-0440            │  Tap → OperatorRemoveFlow
│  Vstavljeno 14.5.2026 09:12         │     with reason = Mistake
│                                     │
│  ───────────────────────────────    │
│  ZAPADLI PREVZEMI  (1)              │  Same component, warning
│  ⚠ Petra Logar                 →    │  tint
│    Sklep · DOK-2026-0398            │  Tap → OperatorRemoveFlow
│    Zapadlo 12.5.2026                │     with reason = Expired
└─────────────────────────────────────┘
```

**Data contract** (new):

```kotlin
data class OperatorPaketnikContext(
    val paketnik: PaketnikInfo,
    val awaitingInsertion: List<PickupSummary>,   // documents queued for THIS paketnik
    val inLocker: List<PickupSummary>,            // pickups currently in this paketnik
    val expiredInLocker: List<PickupSummary>,     // expired pickups still in this paketnik
)

data class PaketnikInfo(val id: String, val name: String, val location: String)
data class PickupSummary(
    val id: String,
    val recipientFirstName: String,
    val recipientLastName: String,
    val documentId: String,
    val documentTitle: String,
    val status: PickupStatus,
    val insertedAt: Instant? = null,
    val expiresAt: Instant? = null,
)
```

The repository:

```kotlin
interface OperatorPickupRepository {
    suspend fun fetchPaketnikContext(paketnikId: String): Result<OperatorPaketnikContext>
    suspend fun overviewCounts(): Result<OperatorOverviewCounts>
    suspend fun markInserted(paketnikId: String, pickupId: String, boxNumber: String): Result<Unit>
    suspend fun markRemoved(pickupId: String, reason: RemovalReason): Result<Unit>
}

enum class RemovalReason { Mistake, Expired }
```

---

## Screen 3 — `OperatorInsertFlow(paketnikId, pickupIds)`

A stepper that walks the Operator through each selected pickup one at a time. Each step:

```
┌─────────────────────────────────────┐
│  ←  Dokument 1 od 3                 │
├─────────────────────────────────────┤
│  Prejemnik:    Marko Horvat         │
│  Dokument:     Diploma              │
│  ID:           DOK-2026-0451        │
│                                     │
│  [Odpri prazen predalček]           │  Reuses UnlockRoute machinery
│                                     │  via the existing
│                                     │  LockerRepository / Direct4Me
│  After box opens:                   │  integration.
│  Vstavite dokument v predalček      │
│  in ga zaprite.                     │
│                                     │
│  [Potrdi vstavitev]                 │  → markInserted(...) →
│                                     │    next step, or Done
└─────────────────────────────────────┘
```

**State machine** (per step):

```
PendingOpen → Opening → AwaitingInsert → Confirming → Done → (next step | Finished)
                  ↓                            ↓
               OpenFailed                  ConfirmFailed
```

If a step fails irrecoverably (e.g., the box wouldn't open), the Operator can **Skip** the doc — it stays in `awaitingInsertion` and they can retry later. The session continues to the next selected doc.

---

## Screen 4 — `OperatorRemoveFlow(paketnikId, pickupId, reason)`

The mirror of the insertion step. One pickup at a time (no batching — the Operator clicks one pickup at a time on the paketnik screen):

```
┌─────────────────────────────────────┐
│  ←  Odstrani prevzem                │
├─────────────────────────────────────┤
│  Prejemnik:    Marko Horvat         │
│  Dokument:     Diploma              │
│  Predalček:    7                    │
│  Razlog:       Operaterjeva napaka  │  (or "Zapadlo")
│                                     │
│  [Odpri predalček]                  │  UnlockRoute mechanics
│                                     │
│  Vzemite dokument iz predalčka      │
│  in ga zaprite.                     │
│                                     │
│  [Potrdi odstranitev]               │  → markRemoved(reason) →
│                                     │    back to OperatorPaketnik
└─────────────────────────────────────┘
```

**Status transitions:**

| Before | Reason | After |
|---|---|---|
| `InLocker` | `Mistake` | `Awaiting` (re-enters insert list) |
| `Expired` | `Expired` | `WaitingPersonalPickup` (terminal until in-person resolution) |

---

## Domain model changes

```kotlin
// New, in domain/pickup/
enum class PickupStatus {
    Awaiting,                  // assigned to org, not in any locker
    InLocker,                  // operator inserted, waiting for recipient
    Removed,                   // transient — used in audit log
    Expired,                   // recipient missed pickup window, still in locker
    WaitingPersonalPickup,     // operator removed an expired doc, awaiting in-person handover
    Collected,                 // recipient picked up (terminal)
}
```

`AuditLogStatus` (existing in `ActivePickupsScreen.kt`) gains two values: `Inserted`, `Removed`. The Zgodovina tab for an Operator shows entries with all five statuses (Confirmed, Opened, Inserted, Removed, plus future).

---

## SessionStore — multi-profile support

Today's `PersistedSessionStore` holds **one** `AppUser` under `auth.persisted_user`. Multi-profile changes that.

**New interface:**

```kotlin
interface SessionStore {
    val session: StateFlow<AuthSession>
    val profiles: StateFlow<List<AppUser>>           // ALL profiles provisioned on this device
    suspend fun hydrate()
    suspend fun addProfile(user: AppUser)            // called from ConfirmAccountRoute
    suspend fun switchProfile(userId: String)        // called from Profile tab dropdown
    suspend fun removeProfile(userId: String)        // "Pozabi ta profil"
    suspend fun setAuthenticated(userId: String)     // promotes a known profile to Authenticated
    suspend fun clear()                              // sign-out (in-memory only)
    suspend fun forgetAllIdentities()                // "Ponastavi napravo" → wipes everything
    suspend fun activeProfile(): AppUser?            // replaces persistedUser()
}
```

**Storage layout** (under `SecureStorage`):
- `auth.persisted_profiles` → `List<AppUser>` JSON
- `auth.active_profile_id` → `String?` — id of the most recently authenticated profile

**Device key:** unchanged. The ECDSA key + PIN salt in `SecureStorage` remain device-scoped. The active profile determines *who* the device-signed challenge represents. Independent per-profile keys are out of scope for this design.

**ConfirmAccountRoute** call site becomes `addProfile(user)` + `setAuthenticated(user.id)`.

**LoginRoute** call site becomes `setAuthenticated(activeProfile().id)` after biometric/PIN success — defaulting to the previously active profile.

---

## Profile tab

```
┌─────────────────────────────────────┐
│  Profil                             │
├─────────────────────────────────────┤
│  ┌──────────────────────────────┐   │
│  │ Marko Horvat            ▼    │   │  EDropdown
│  │ Operater · UE Ljubljana      │   │
│  └──────────────────────────────┘   │
│       ┌────────────────────────┐    │
│       │ ● Marko Horvat         │    │
│       │   Operater · UE Lj.    │    │
│       │ ○ Ana Novak            │    │
│       │   Skrbnik · MNZ        │    │
│       │ ○ + Dodaj profil       │    │  → RegistrationCode flow
│       └────────────────────────┘    │
│                                     │
│  PODATKI                            │
│  Ime in priimek    Marko Horvat     │
│  E-pošta           marko@gov.si     │
│  Vloga             Operater         │
│  Organizacija      UE Ljubljana     │
│                                     │
│  [Odjava]                           │  sessionStore.clear()
│  [Pozabi ta profil]                 │  sessionStore.removeProfile(...)
│  [Ponastavi napravo]                │  securityRepository.reset() +
│                                     │   sessionStore.forgetAllIdentities()
└─────────────────────────────────────┘
```

Switching profile in the dropdown calls `switchProfile(userId)` and then `setAuthenticated(userId)`. Because the biometric/PIN already succeeded at app open, the switch does **not** re-prompt — the device is already unlocked. (If we ever want per-profile re-prompts, that's a future extension.)

---

## Component reuse

| Existing component | Reused for |
|---|---|
| `EScaffold`, `ETopBar`, `EBottomNavigationBar` | Both home screens |
| `EPickupCard` | All three lists in `OperatorPaketnik` (needs a `trailingCheckbox: Boolean` variant) |
| `EDetailsSectionLabel`, `EDetailsCard`, `EDetailsRow` | Profile tab "Podatki" section |
| `UnlockRoute` + `LockerRepository` | Open-box mechanic in both insert and remove flows |
| `EPrimaryButton`, `EInfoCard`, `EStatusChip` | Throughout |

One new component is needed: a multi-line dropdown that shows label + subtitle per option (`EProfileDropdown` or generalize to `ESelectField`).

---

## Phasing

Suggested implementation order (each phase ships independently):

1. **Phase 1 — Routing split.** `App.kt` branches authenticated users into a new `OperatorHomeRoute` stub vs. existing `ActivePickupsRoute`. Operator stub is a "Skeniraj QR kodo" screen that doesn't yet scan.
2. **Phase 2 — Multi-profile `SessionStore`.** Refactor persistence layout. Migrate the single `auth.persisted_user` to `auth.persisted_profiles` (with a one-time read of the old key for in-place migration).
3. **Phase 3 — Paketnik screen + fake operator repo.** Static fixtures in `FakeOperatorPickupRepository`. Selection, sections, navigation work end-to-end with fakes.
4. **Phase 4 — Insertion + removal flows.** Wire `UnlockRoute` into the new flows. Use the fake repo to flip statuses.
5. **Phase 5 — Profile tab dropdown + audit log additions.** Wire the dropdown to `SessionStore`. Extend `AuditLogStatus`.
6. **Phase 6 — Real backend integration.** Replace fakes once backend endpoints land. Out of scope here.

---

## Open questions

1. **Box selection during insertion**: does the Operator choose which physical box (1..N) to use, or does the backend / paketnik allocate the next free one and tell the app which one to open? The current `Direct4MeLockerRepository` opens a specific box, so the API needs to know. Assume backend allocates for now.
2. **Concurrent provisioning**: can two Operators be logged into the same device simultaneously, or only sequentially? Design assumes sequential — switching is a state change, not a multi-tenant view.
3. **Document title source**: `PickupSummary.documentTitle` — is this a fixed taxonomy ("Diploma", "Izpis", "Sklep") or free-form? Assume free-form for now.

These can be resolved during implementation planning or backend contract design — they don't block this UI design.
