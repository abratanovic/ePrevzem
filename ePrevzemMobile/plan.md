# Plan: Active Pickups + Pickup Details screens

## Context

The app currently shows only the Welcome / RegistrationCode onboarding flow. The next two screens to build are:

- **Active Pickups** (`Aktivni prevzemi`) — the home screen listing all pending document pickups, with 5 states: populated (normal), one-expiring-soon, refreshing, empty, and bottom-nav
- **Pickup Details** (`Podrobnosti prevzema`) — a detail screen showing full pickup info, location map placeholder, security verification, and the full unlock flow (confirm → biometric/PIN sheet → unlocked countdown → picked-up confirmation)

Both screens use only the existing E* design-system; no raw Material3 calls.

---

## Files to create

### Data models
- `feature/pickups/model/Pickup.kt`
  - `data class PickupItem(id, title, organization, location, lockerNumber, deadline, status: EPickupStatus, isExpiringSoon)`
  - `data class PickupDetails(id, title, organization, reference, type, availableFrom, deadline, deadlineFormatted, status, isExpiringSoon, locationName, locationAddress, lockerNumber, unlockedAt?)`
  - `enum class UnlockPhase { Idle, Unlocked, Confirmed }`

### Active Pickups feature (`feature/pickups/`)
- `ActivePickupsState.kt` — `@Immutable data class ActivePickupsState(userName, pickups: List<PickupItem>, isRefreshing, activeTab: ActiveTab)` + `enum class ActiveTab { Pickups, History, Profile }`
- `ActivePickupsEvent.kt` — `sealed interface ActivePickupsEvent` with: `Refresh`, `PickupClicked(id)`, `TabSelected(tab)`
- `ActivePickupsScreen.kt` — stateless `ActivePickupsScreen` + stateful `ActivePickupsRoute`

### Pickup Details feature (`feature/pickups/`)
- `PickupDetailsState.kt` — `@Immutable data class PickupDetailsState(details, showUnlockDialog, showBiometricSheet, showPinSheet, pinValue, unlockPhase, secondsRemaining)`
- `PickupDetailsEvent.kt` — `sealed interface PickupDetailsEvent` with: `Back`, `Share`, `UnlockClicked`, `UnlockConfirmed`, `UnlockCancelled`, `BiometricSelected`, `PinSelected`, `PinDigitEntered(digit)`, `PinBackspace`, `Finish`, `LockerDidNotOpen`
- `PickupDetailsScreen.kt` — stateless `PickupDetailsScreen` + stateful `PickupDetailsRoute`

### Pickup Confirmed screen (`feature/pickups/`)
- `PickupConfirmedState.kt` — `data class PickupConfirmedState(details: PickupDetails)`
- `PickupConfirmedEvent.kt` — `sealed interface PickupConfirmedEvent` with: `Finish`
- `PickupConfirmedScreen.kt` — stateless `PickupConfirmedScreen` + `PickupConfirmedRoute`

### New design-system components
- `core/designsystem/components/feedback/EAlertBanner.kt`
  - `enum class EAlertType { Warning, Error, Info }`
  - `fun EAlertBanner(type, title, modifier, message?, icon?)` — same structure as existing `EErrorBanner` but parametrized (uses `warningBg`/`warning` for Warning, `errorBg`/`error` for Error, `infoBg`/`info` for Info)
  - Keep the existing `EErrorBanner` as-is (no breaking changes)
- `core/designsystem/components/inputs/EPinPad.kt`
  - `fun EPinPad(value: String, length: Int = 6, onDigit: (Int)->Unit, onBackspace: ()->Unit, onSwitchToFallback: (()->Unit)?, switchFallbackLabel: String?, modifier)`
  - Renders: 6-dot display row (filled/empty circles) + 3×4 number grid + "0" + backspace key
  - Pure display + callback; no internal state

### New drawable
- `composeResources/drawable/ic_share.xml` — Material Symbols "share" (box-with-arrow-up), 24×24dp vector

---

## Files to modify

### `EPrevzemIcons.kt`
Add one line:
```kotlin
@Composable fun share(): Painter = painterResource(Res.drawable.ic_share)
```

### `EPickupCard.kt`
- Add `lockerNumber: String` param — render below the location `IconText` in the footer column
- Add `warningText: String? = null` param — when non-null, append an amber `EAlertBanner(Warning)` at the bottom of the card (outside the main padding, inside the card clip)
- Update footer layout: left column (location icon+text, locker number text), right cell (expires)

### `ETopBar.kt`
Three backward-compatible additions:
1. `leadingIcon: Painter? = null` — when set in Home variant, renders an `EIconChip` (44 dp, Green tint) before the title column (showing `ic_organization` for the institution badge)
2. `userInitials: String? = null` — when set, renders an initials text circle (36 dp, white 0.12 alpha bg) on the right **instead of** the actionIcon circle; ignored when null (falls back to existing behaviour)
3. Detail variant: render `eyebrow` above `title` when `eyebrow` is non-null (currently only Home does this)

### `EConfirmationDialog.kt`
Add `content: (@Composable () -> Unit)? = null` slot rendered between the message text and the button stack. Used by the unlock confirmation to inject the "Paketnik #..." locker chip. Existing callers unaffected (parameter defaults to null).

### `App.kt`
Extend `AppDestination`:
```kotlin
data object ActivePickups : AppDestination
data class PickupDetails(val pickupId: String) : AppDestination
data object PickupConfirmed : AppDestination
```
Wire: `onCodeAccepted → ActivePickups`; `ActivePickups.onPickupClicked(id) → PickupDetails(id)`; `PickupDetails.onBack → ActivePickups`; `PickupDetails.onPickupConfirmed → PickupConfirmed`; `PickupConfirmed.onFinish → ActivePickups`

---

## Screen layout details

### ActivePickupsScreen
```
EScaffold(
  topBar = ETopBar(Home, leadingIcon=organization(), userInitials="AH"),
  bottomBar = EBottomNavigationBar(tabs=[Prevzemi,Zgodovina,Profil], active=Pickups)
) {
  EScreen {
    // Welcome header
    Text("DOBRODOŠLI NAZAJ", caption, muted)
    Text("Pozdravljeni, $userName", display)

    // Section row
    Row {
      Text("Aktivni prevzemi", section)
      Text("$count aktivni", bodySmall, muted)
      Spacer(weight 1f)
      RefreshButton (icon button, refresh icon)
    }

    // Refreshing inline state
    if (isRefreshing) ELoadingState("Osvežujem seznam …")

    // Empty state
    if (!isRefreshing && pickups.isEmpty())
      EEmptyState(icon=locker(), "Trenutno nimate aktivnih prevzemov.", "Ko bo organizacija...")

    // List
    pickups.forEach { pickup ->
      EPickupCard(
        title=pickup.title, organization=..., location=..., expires=...,
        lockerNumber=pickup.lockerNumber, status=pickup.status,
        warningText = if (pickup.isExpiringSoon) "Manj kot 24 ur do izteka roka prevzema." else null,
        onClick = { onEvent(PickupClicked(pickup.id)) }
      )
    }
  }
}
```

### PickupDetailsScreen (Idle phase)
```
EScaffold(
  topBar = ETopBar(Detail, eyebrow="EPREVZEM", title="Podrobnosti prevzema",
                   onBack=Back, actionIcon=share(), onAction=Share),
) {
  EScreen {
    EStatusChip(details.status)
    Text(details.title, display)
    Row { Icon(organization,14dp) Text(details.organization, bodySmall) }

    // Expiry warning (if isExpiringSoon)
    EAlertBanner(Warning, "Manj kot 24 ur do izteka roka.",
                 "Po izteku roka prevzem ni več možen. Dokument bo vrnjen pošiljatelju.",
                 icon=clock())

    // Podrobnosti card
    ESummaryCard(title="Podrobnosti", icon=document()) {
      DetailRow("Referenca", details.reference)
      DetailRow("Vrsta", details.type)
      DetailRow("Organizacija", details.organization)
      DetailRow("Na voljo od", details.availableFrom)
      DetailRow("Prevzem do", details.deadlineFormatted)   // orange text if expiring
      DetailRow("Status") { EStatusChip(details.status) }
    }

    // Lokacija card (with locker chip in header trailing slot)
    ESummaryCard(title="Lokacija", icon=location()) {
      LockerChip("Paketnik #...")   // pill: lock icon + number text
      MapPlaceholder()              // Box surfaceSunken bg + location pin icon + label
      Text(details.locationName, cardTitle)
      Text(details.locationAddress, bodySmall, textSecondary)
    }

    // Varnostna preverba card
    ESummaryCard(title="Varnostna preverba", icon=shieldOutlined()) {
      Text(description, body, textSecondary)
      VerificationOptionRow(icon=biometric(), label="Biometrija (Face / Touch ID)")
      VerificationOptionRow(icon=key(), label="6-mestni PIN ePrevzem")
    }

    // Kako poteka prevzem card
    ESummaryCard(title="Kako poteka prevzem", icon=info()) {
      NumberedStep(1, "Tapnite »Odkleni predalček«.")
      NumberedStep(2, "Potrdite identiteto z biometrijo ali PIN-om.")
      NumberedStep(3, "Predalček se bo odprl za 30 sekund.")
      NumberedStep(4, "Vzemite vsebino in zaprite vratca.")
    }

    EPrimaryButton(icon=lock(), "Odkleni predalček", onClick=UnlockClicked)
    ESecondaryButton(icon=profile(), "Pooblasti drugo osebo")
    ETextButton("Imate težave?")
  }
}

// Overlays (rendered outside EScaffold content)
if (showUnlockDialog) EConfirmationDialog(
  icon=lock(), title="Odkleni predalček?",
  message="...",
  content = { LockerChip("Paketnik #...") },
  confirmLabel="Da, odkleni", dismissLabel="Prekliči",
  onConfirm=UnlockConfirmed, onDismiss=UnlockCancelled
)
if (showBiometricSheet) EBottomSheet(onDismiss=UnlockCancelled) {
  Text("PREVERJANJE IDENTITETE", caption)
  BiometricSpinner()   // 80dp circle + CircularProgressIndicator arc + biometric icon
  Text("Preverjamo identiteto …", section)
  ETextButton("Uporabi PIN namesto biometrije", onClick=PinSelected)
  ESecondaryButton("Prekliči", onClick=UnlockCancelled)
}
if (showPinSheet) EBottomSheet(onDismiss=UnlockCancelled) {
  Text("PREVERJANJE IDENTITETE", caption)
  Text("Vnesite PIN ePrevzem", section)
  EPinPad(pinValue, onDigit=PinDigitEntered, onBackspace=PinBackspace,
          onSwitchToFallback=BiometricSelected, switchFallbackLabel="Uporabi biometrijo")
  ESecondaryButton("Prekliči", onClick=UnlockCancelled)
}
```

### PickupDetailsScreen (Unlocked phase — state within PickupDetails)
Triggered when `unlockPhase == UnlockPhase.Unlocked`. Countdown runs via `LaunchedEffect` in Route.
```
EScaffold (no topBar) {
  Column(center, fillMaxSize) {
    UnlockIconBubble()   // 80dp primary50 circle + unlock icon
    Text("Predalček je odklenjen", display, centered)
    Text("Prevzemite dokument iz predalčka.", body, centered)
    CountdownChip("Predalček bo odprt še $secondsRemaining s")
    UnlockedSummaryCard(details)   // title, org, "Odklenjeno" chip, location, unlockedAt
    Spacer(weight 1f)
    EPrimaryButton(icon=check(), "Končaj", onClick=Finish)   // → onPickupConfirmed
    ETextButton("Predalček se ni odprl", onClick=LockerDidNotOpen)
  }
}
```

### PickupConfirmedScreen (separate destination)
```
EScaffold (no topBar) {
  Column(center, fillMaxSize) {
    SuccessIconBubble()  // 80dp primary50 circle + success (check) icon in primary color
    Text("Dokument je prevzet", display, centered)
    Text("Hvala. Prevzem je uspešno zaključen.", body, centered)
    ConfirmedSummaryCard(details)  // title, org, "Prevzeto" chip, location, unlockedAt
    Spacer(weight 1f)
    EPrimaryButton(icon=check(), "Končaj", onClick=Finish)   // → ActivePickups
  }
}
```

---

## Route sample data
`ActivePickupsRoute` uses hardcoded `remember { mutableStateOf(...) }` with 3 sample pickups (Osebna izkaznica/Ready, Diploma/Expiring, Potrdilo/Ready) so all states are exercisable. `PickupDetailsRoute` seeds from the clicked pickup id.

---

## Commit breakdown

**Commit 1 — Design system additions**
- `ic_share.xml` drawable + `EPrevzemIcons.share()`
- `EAlertBanner.kt` (warning / error / info parametrized banner)
- `EConfirmationDialog.kt` — add `content` slot
- `EPickupCard.kt` — add `lockerNumber` + `warningText`
- `ETopBar.kt` — add `leadingIcon`, `userInitials`, eyebrow in Detail variant
- `EPinPad.kt` — numeric PIN pad component

**Commit 2 — Active Pickups screen**
- `feature/pickups/model/Pickup.kt` (shared data models)
- `ActivePickupsState.kt`, `ActivePickupsEvent.kt`, `ActivePickupsScreen.kt`
- `App.kt` wired for `ActivePickups` destination

**Commit 3 — Pickup Details + Confirmed screens**
- `PickupDetailsState.kt`, `PickupDetailsEvent.kt`, `PickupDetailsScreen.kt`
- `PickupConfirmedState.kt`, `PickupConfirmedEvent.kt`, `PickupConfirmedScreen.kt`
- `App.kt` — add `PickupDetails` and `PickupConfirmed` destinations

No Co-Authored-By footer on any commit.

---

## Verification
1. `./gradlew :composeApp:compileCommonMainKotlinMetadata` — must pass after each commit
2. Visually inspect Active Pickups in all 5 states (populated, expiring, refreshing, empty, bottom-nav selection)
3. Tap a card → PickupDetails; exercise all overlays (confirm dialog, biometric sheet, PIN sheet), unlock phase, and confirmed screen
4. Verify ETopBar Home variant shows org icon left + initials circle right; Detail variant shows eyebrow + title
