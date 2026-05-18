# QR locker unlock — design

**Status:** Draft
**Date:** 2026-05-11
**Scope:** Android target only. iOS actuals are stubbed so the project still compiles; full iOS implementation is a follow-up.

## Goal

Replace the simulated unlock at the end of `PickupDetailsRoute` with a real unlock against the direct4.me sandbox API. After the existing biometric/PIN identity check passes, the user scans the locker's QR code, the app POSTs the locker ID to direct4.me, decodes the returned audio token, and plays the WAV through the phone speaker. When the audio finishes, the existing "Predalček je odklenjen" screen is shown.

The full end-to-end flow becomes:

1. Pickup details → tap "Odkleni predalček"
2. Existing confirmation dialog → biometric or PIN sheet (unchanged)
3. Identity confirmed → **new** QR scanner screen
4. QR matches pickup's `lockerNumber` → **new** "Odklepanje…" screen (API call + WAV playback)
5. Playback finishes → existing `UnlockedPhase` ("Predalček je odklenjen")

## Non-goals

- iOS implementation (camera, audio, permissions). iOS `expect` declarations exist but `actual`s throw `NotImplementedError`.
- Telemetry / analytics.
- Accessibility beyond `contentDescription` parity with existing screens.
- Translating per-`errorNumber` API failures into user-facing messages (generic message only).
- Persisting unlock history.
- Refreshing or rotating the direct4.me API key.

## Architecture

A new feature package owns the QR → API → audio orchestration. The data and platform-glue pieces live alongside the existing structure:

```
si.mentis.eprevzemmobile
├── data/
│   └── locker/
│       ├── LockerRepository.kt           # interface + sealed OpenBoxResult
│       ├── Direct4MeLockerRepository.kt  # Ktor implementation
│       ├── FakeLockerRepository.kt       # in-memory stub for previews / dev
│       └── dto/
│           ├── OpenBoxRequest.kt
│           └── OpenBoxResponse.kt
├── core/
│   ├── camera/
│   │   ├── QrScanner.kt                  # expect class
│   │   ├── QrScanner.android.kt          # CameraX + ML Kit Barcode actual
│   │   └── QrScanner.ios.kt              # throws NotImplementedError
│   └── audio/
│       ├── TokenAudioPlayer.kt           # expect class
│       ├── TokenAudioPlayer.android.kt   # MediaPlayer actual
│       └── TokenAudioPlayer.ios.kt       # throws NotImplementedError
└── feature/
    └── unlock/
        ├── UnlockRoute.kt
        ├── UnlockScreen.kt
        ├── UnlockState.kt
        └── UnlockEvent.kt
```

`AppContainer` gains a `lockerRepository: LockerRepository` provided by `Direct4MeLockerRepository(baseUrl, apiKey)`. The API key is read from `BuildConfig.DIRECT4ME_API_KEY` on Android and passed in via Android-side wiring; commonMain receives it as a constructor argument so commonMain stays platform-agnostic. The sandbox base URL is hardcoded for now: `https://api-d4me-stage.direct4.me/sandbox/v1/`.

### Navigation change

`App.kt` adds a destination:

```
data class Unlock(val pickupId: String, val lockerNumber: String) : AppDestination
```

`PickupDetailsEvent` gains `IdentityVerified` (emitted when biometric simulation finishes or the 6-digit PIN is entered). `PickupDetailsRoute` no longer flips its own state to `UnlockPhase.Unlocked` — it calls a new `onIdentityVerified` callback. `App.kt` transitions to `AppDestination.Unlock` in response. On unlock success, `App.kt` flips the pickup details state to `Unlocked` (with `unlockedAt`) and routes back to `PickupDetailsRoute`'s existing `UnlockedPhase`. On cancel, it routes back without changes.

The existing "Predalček se ni odprl" text button in `UnlockedPhase` re-enters `AppDestination.Unlock` for a retry.

## Components & data flow

### UnlockState

```kotlin
data class UnlockState(
    val pickupId: String,
    val expectedLockerNumber: String,
    val phase: UnlockPhase,
    val attempt: Int = 0,
)

sealed interface UnlockPhase {
    data object RequestingPermission : UnlockPhase
    data object Scanning : UnlockPhase
    data class ScanError(val reason: ScanErrorReason) : UnlockPhase
    data object Unlocking : UnlockPhase                      // POST in flight OR WAV playing
    data class Failed(val error: UnlockError) : UnlockPhase  // recoverable until attempt == MAX
    data object Unlocked : UnlockPhase                       // brief terminal state before callback
}

enum class ScanErrorReason { InvalidPayload, WrongLocker, CameraDenied }
sealed interface UnlockError {
    data object Network : UnlockError
    data class Api(val errorNumber: Int) : UnlockError
    data object PlaybackFailed : UnlockError
}
```

`MAX_UNLOCK_ATTEMPTS = 3`. After three `Failed` results the retry button is replaced with "Pokliči podporo" + "Nazaj na podrobnosti".

### UnlockEvent

```kotlin
sealed interface UnlockEvent {
    data object Back : UnlockEvent
    data object PermissionGranted : UnlockEvent
    data object PermissionDenied : UnlockEvent
    data object OpenSettings : UnlockEvent
    data class QrDetected(val raw: String) : UnlockEvent
    data object DismissScanError : UnlockEvent
    data object Retry : UnlockEvent
    data object ContactSupport : UnlockEvent
}
```

### Flow

1. **Mount.** `UnlockRoute` enters `RequestingPermission`. The Android actual `QrScanner.requestPermission()` triggers the system camera permission prompt. Granted → `PermissionGranted` → `Scanning`. Denied → `ScanError(CameraDenied)`.
2. **Scanning.** Full-screen camera preview from `QrScanner.preview()`. Overlay: rounded-square viewfinder cutout using `EPrevzemTheme.shapes`, Slovenian helper "Skenirajte QR kodo na predalčku" below the cutout, `ETopBar` (Detail variant) with back arrow that emits `Back`. `QrScanner` emits a `Flow<String>` of decoded payloads; the route consumes the first non-blank value and stops the scanner.
3. **Validate.** Parse the QR payload to `Long`. Failure → `ScanError(InvalidPayload)`. If parsed value ≠ `expectedLockerNumber.toLongOrNull()` → `ScanError(WrongLocker)`. Both surface as `EAlertBanner` overlaid on the (paused) viewfinder with a primary "Poskusite znova" button that returns to `Scanning`.
4. **Unlocking.** Show full-screen loading composable: centered `CircularProgressIndicator`, "Odklepanje…" `typography.display`, helper "Telefon predvaja zvočni signal. Približajte ga predalčku." in `typography.bodySmall`. Disable back navigation. Call `lockerRepository.openBox(boxId)`; on `Success`, `TokenAudioPlayer.play(bytes)` suspends until playback completes. After playback returns → `Unlocked` → invoke `onUnlocked(unlockedAt)` callback.
5. **Failure.** Network exception → `Failed(Network)`. Non-zero `result`/`errorNumber` → `Failed(Api(errorNumber))`. `MediaPlayer` error → `Failed(PlaybackFailed)`. Show generic error screen using `EAlertBanner` + primary "Poskusite znova" button (increments `attempt`, runs the POST again). After `attempt == MAX_UNLOCK_ATTEMPTS`, swap the retry button for "Pokliči podporo" and a secondary "Nazaj".

### LockerRepository

```kotlin
interface LockerRepository {
    suspend fun openBox(boxId: Long, tokenFormat: Int = 1): OpenBoxResult
}

sealed interface OpenBoxResult {
    data class Success(val tokenWavBytes: ByteArray) : OpenBoxResult
    data class ApiFailure(val errorNumber: Int) : OpenBoxResult
    data class NetworkFailure(val cause: Throwable) : OpenBoxResult
}
```

`Direct4MeLockerRepository`:

- POSTs `{"boxId": <Long>, "tokenFormat": <Int>}` to `Access/openbox`.
- Adds the API key as a default request header. Header name to be confirmed against direct4.me docs at implementation time; if unknown, try `Authorization: Bearer <key>` first.
- Parses the JSON response with kotlinx.serialization.
- On `result == 0` and `errorNumber == 0`, decodes `data`: base64 → gzip-compressed bytes → gunzip → raw WAV bytes. Returns `Success(wavBytes)`.
- On `result != 0` returns `ApiFailure(errorNumber)`.
- On thrown exception (timeout, IO, parse failure) returns `NetworkFailure(cause)`.
- 15-second Ktor request timeout.

`FakeLockerRepository`: returns a small pre-encoded WAV (e.g., a 200 ms sine tone bundled as a resource) for previews and offline development.

### Platform glue

`QrScanner` (expect class):

```kotlin
expect class QrScanner {
    suspend fun requestPermission(): Boolean
    fun openAppSettings()
    @Composable fun Preview(modifier: Modifier, onResult: (String) -> Unit)
    fun stop()
}
```

Android actual: holds a `CameraX` `ProcessCameraProvider`, binds `Preview` + `ImageAnalysis` use cases, runs ML Kit Barcode (`BarcodeScanning.getClient(BarcodeScannerOptions(format = QR_CODE))`) on each frame, invokes `onResult` with the first decoded payload, then stops. Permissions handled via `rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission())`.

`TokenAudioPlayer` (expect class):

```kotlin
expect class TokenAudioPlayer {
    suspend fun play(wavBytes: ByteArray)   // returns when playback completes; throws on error
}
```

Android actual: writes bytes to a `File(cacheDir, "unlock-token.wav")`, configures `MediaPlayer` for `STREAM_MUSIC` at maximum app volume, starts playback, and `suspendCancellableCoroutine`s on `OnCompletionListener` / `OnErrorListener`. Releases the `MediaPlayer` in the coroutine cleanup path. The cache file is overwritten on each call (never persisted long-term).

iOS actuals for both: throw `NotImplementedError("iOS QR/audio not yet implemented")`. App still compiles for the iOS framework target.

## UI

All composables follow the `E*` design system. No raw Material3 except where already used (e.g., `CircularProgressIndicator`, consistent with `PickupDetailsScreen`).

**Scanning screen.**
- `EScaffold` with `ETopBar` (Detail variant, eyebrow "EPREVZEM", title "Skeniranje", back arrow).
- Body: black background, `QrScanner.Preview` filling the body. Centered viewfinder cutout (80% width, square, `EPrevzemTheme.shapes.large` corner radius, transparent inside, semi-opaque scrim outside). Below the cutout: `EAlertBanner`-style info row with `EPrevzemIcons.info()` and helper text.

**Scan error overlay.**
- Same scaffold as scanning, but the scanner is paused. An `EAlertBanner(type = Error)` is placed below the viewfinder with the relevant message and a single `EPrimaryButton("Poskusite znova")` returning to `Scanning`.

**Unlocking screen.**
- `EScaffold` (no top bar — back is intentionally disabled here).
- Centered column: large `CircularProgressIndicator`, `Text("Odklepanje…", typography.display)`, helper text in `typography.bodySmall` `colors.textSecondary`.

**Failure screen.**
- `EScaffold` with `ETopBar` (Detail variant, title "Odklep ni uspel", back returns to pickup details).
- Centered column: error icon bubble (reuse the `UnlockIconBubble` pattern but with `EPrevzemIcons.warning()` and `colors.errorBg`), `Text("Odklep ni uspel")`, helper "Poskusite znova ali kontaktirajte podporo.".
- Action area: while `attempt < 3` show `EPrimaryButton("Poskusite znova")` + `ETextButton("Prekliči")`. Once `attempt >= 3` swap to `EPrimaryButton("Pokliči podporo")` + `ESecondaryButton("Nazaj na podrobnosti")`.

**Camera-denied screen.**
- Same shell as failure. Title "Dostop do kamere", helper "Za skeniranje QR kode potrebujemo dostop do kamere.". Buttons: `EPrimaryButton("Odpri nastavitve")` (calls `QrScanner.openAppSettings()`), `ESecondaryButton("Prekliči")`.

All Slovenian copy stays in UI; code identifiers stay English.

## Error handling

| Condition | UI state | User action available |
|---|---|---|
| Camera permission denied | `ScanError(CameraDenied)` | "Odpri nastavitve" / "Prekliči" |
| QR payload not a number | `ScanError(InvalidPayload)` | "Poskusite znova" → re-scan |
| QR boxId ≠ pickup.lockerNumber | `ScanError(WrongLocker)` | "Poskusite znova" → re-scan |
| Network exception, timeout | `Failed(Network)` | "Poskusite znova" up to 3 times |
| API `result != 0` | `Failed(Api(errorNumber))` | same; raw `errorNumber` is logged, not shown |
| MediaPlayer `onError` | `Failed(PlaybackFailed)` | same |
| Back press during `Unlocking` | ignored | — |
| App backgrounded mid-unlock | `UnlockRoute`'s `DisposableEffect` cancels the coroutine; returning to the app shows pickup details | — |

The `attempt` counter increments only on entering `Failed`. Re-scanning after a `ScanError` does not consume an attempt.

## Dependencies

Additions to `gradle/libs.versions.toml`:

- **Ktor client** (`ktor-client-core`, `ktor-client-content-negotiation`, `ktor-serialization-kotlinx-json`) — commonMain. Engine in `androidMain` (`ktor-client-okhttp` or `ktor-client-android`), iOS engine deferred.
- **kotlinx.serialization** (`kotlinx-serialization-json`) — commonMain.
- **kotlinx.coroutines** — already transitively available via Compose, but add an explicit `kotlinx-coroutines-core` entry if missing.
- **CameraX** (`androidx.camera:camera-core`, `camera-camera2`, `camera-lifecycle`, `camera-view`) — androidMain only.
- **ML Kit Barcode** (`com.google.mlkit:barcode-scanning`) — androidMain only.

The `kotlin-plugin-serialization` Gradle plugin is added to `composeApp/build.gradle.kts`. `AndroidManifest.xml` gains:

```
<uses-permission android:name="android.permission.CAMERA" />
<uses-feature android:name="android.hardware.camera" android:required="false" />
<uses-permission android:name="android.permission.INTERNET" />
```

`local.properties` (git-ignored) gains `direct4me.api.key=...`. `composeApp/build.gradle.kts` reads it and emits `BuildConfig.DIRECT4ME_API_KEY` via the AGP `buildConfigField` mechanism (`buildFeatures.buildConfig = true`).

## Open questions to confirm during implementation

- Exact header name and format expected by direct4.me for the API key.
- Whether `data` is always gzip-compressed (the sample payload's `H4sI` prefix suggests yes) or sometimes raw base64 WAV; if uncertain, sniff the gzip magic bytes (`0x1f 0x8b`) and fall back to raw bytes if absent.
- The actual `lockerNumber` format on real pickups — currently a free-form `String`. If real values include letters/prefixes, `toLongOrNull()` matching will need a parsing rule.
