# Real Interactive Map in Pickup Location Section — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the fake grid `MapPlaceholder` in the pickup details "Lokacija" section with a real interactive native map (osmdroid on Android, Apple MapKit on iOS) marking the actual station coordinates.

**Architecture:** The domain `Location` already carries `Latitude`/`Longitude` (decimal). We thread them through the citizen pickup-detail read projection → DTO → mobile model, then render them with a new `EStationMap` design-system component implemented via Kotlin `expect`/`actual` (commonMain declaration; osmdroid `MapView` on Android, `MKMapView` on iOS).

**Tech Stack:** .NET 9 / EF Core (backend read projection), Kotlin Multiplatform + Compose Multiplatform, osmdroid 6.1.18 (Android), Apple MapKit (iOS via UIKitView).

**No integration tests** (per request). Verification is by compilation/build of each affected target.

---

## Parallelization

Four independent tracks. **A, B, C can run fully in parallel** (different files, no shared edits). **D is the only join point** — it needs the model field from B and the component from C.

- **Track A — Backend plumbing** (Tasks 1–2): `backend/` only.
- **Track B — Mobile data plumbing** (Tasks 3–6): mobile DTO + model + repos.
- **Track C — Map component** (Tasks 7–10): Gradle dep, osmdroid config, `EStationMap` expect/actual.
- **Track D — Screen integration** (Task 11): depends on B (Task 4) + C (Task 8).

Coordinate constant used across the mobile side: **Ljubljana center `46.0569, 14.5058`** as the fallback/default.

---

## File Structure

| File | Track | Responsibility |
|------|-------|----------------|
| `backend/ePrevzem.Infrastructure/Pickups/PickupReadRepository.cs` | A | Add `Latitude`/`Longitude` to `CitizenPickupRow` + projection + detail mapping |
| `backend/ePrevzem.Application/Pickups/Dtos/CitizenPickupResponses.cs` | A | Add `Latitude`/`Longitude` to `CitizenPickupDetailResponse` |
| `composeApp/.../data/api/PickupDtos.kt` | B | Add `latitude`/`longitude` to `CitizenPickupDetailDto` |
| `composeApp/.../feature/pickups/model/Pickup.kt` | B | Add `latitude`/`longitude` to `PickupDetails` |
| `composeApp/.../data/pickups/HttpPickupRepository.kt` | B | Map new fields |
| `composeApp/.../data/pickups/FakePickupRepository.kt` | B | Real Ljubljana coords for fakes |
| `gradle/libs.versions.toml` | C | osmdroid version + library alias |
| `composeApp/build.gradle.kts` | C | osmdroid dependency in androidMain |
| `composeApp/.../EPrevzemApp.kt` (androidMain) | C | One-time osmdroid config |
| `composeApp/src/commonMain/.../core/designsystem/components/map/EStationMap.kt` | C | `expect` declaration |
| `composeApp/src/androidMain/.../core/designsystem/components/map/EStationMap.android.kt` | C | osmdroid `actual` |
| `composeApp/src/iosMain/.../core/designsystem/components/map/EStationMap.ios.kt` | C | MapKit `actual` |
| `composeApp/.../feature/pickups/PickupDetailsScreen.kt` | D | Swap `MapPlaceholder` → `EStationMap`, delete placeholder |

---

## Track A — Backend plumbing

### Task 1: Expose coordinates in the citizen detail response DTO

**Files:**
- Modify: `backend/ePrevzem.Application/Pickups/Dtos/CitizenPickupResponses.cs`

- [ ] **Step 1: Add `Latitude`/`Longitude` to `CitizenPickupDetailResponse`**

Edit the `CitizenPickupDetailResponse` record (only this one — leave `CitizenPickupResponse` untouched) to insert two `decimal` parameters right after `LocationAddress`:

```csharp
public sealed record CitizenPickupDetailResponse(
    Guid Id,
    string Reference,
    string Description,
    string OrganizationName,
    string LocationName,
    string LocationAddress,
    decimal Latitude,
    decimal Longitude,
    int? LockerNumber,
    string Status,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PickedUpAt,
    bool IsExpiringSoon);
```

- [ ] **Step 2: Verify it does not yet compile (mapping not updated)**

Run (from repo root): `dotnet build backend/ePrevzem.Application/ePrevzem.Application.csproj`
Expected: PASS (the record alone compiles; the Infrastructure caller breaks in Task 2). If building the whole solution, expect the failing caller in `PickupReadRepository`.

- [ ] **Step 3: Commit**

```bash
git add backend/ePrevzem.Application/Pickups/Dtos/CitizenPickupResponses.cs
git commit -m "feat(api): add coordinates to citizen pickup detail response"
```

---

### Task 2: Populate coordinates in the read projection

**Files:**
- Modify: `backend/ePrevzem.Infrastructure/Pickups/PickupReadRepository.cs`

- [ ] **Step 1: Add the two fields to `CitizenPickupRow`**

In the private `CitizenPickupRow` class (currently ends after `City`), add below `public required string City { get; init; }`:

```csharp
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
```

- [ ] **Step 2: Populate them in `CitizenPickupQuery()`**

In the `select new CitizenPickupRow { ... }`, add right after the `City = claim.Location.City,` line:

```csharp
               Latitude = claim.Location.Latitude,
               Longitude = claim.Location.Longitude,
```

- [ ] **Step 3: Pass them into the detail response**

In `GetCitizenPickupDetailAsync`, the `return new CitizenPickupDetailResponse(...)` currently passes
`x.OrganizationName, x.City, FormatAddress(...),` then `x.LatestLockerNumber, ...`.
Insert `x.Latitude, x.Longitude,` between the `FormatAddress(...)` argument and `x.LatestLockerNumber`:

```csharp
        return new CitizenPickupDetailResponse(
            x.Id.Value,
            x.Reference,
            x.Description,
            x.OrganizationName,
            x.City,
            FormatAddress(x.Address, x.HouseNumber, x.ZipCode, x.City),
            x.Latitude,
            x.Longitude,
            x.LatestLockerNumber,
            x.Status.ToString(),
            x.DeadlineAt,
            x.CreatedAt,
            x.PickedUpAt,
            IsExpiringSoon(x.Status, x.DeadlineAt, now));
```

- [ ] **Step 4: Build the solution**

Run (from repo root): `dotnet build ePrevzem.sln`
Expected: PASS, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Infrastructure/Pickups/PickupReadRepository.cs
git commit -m "feat(api): project station coordinates into citizen pickup detail"
```

---

## Track B — Mobile data plumbing

### Task 3: Add coordinates to the mobile detail DTO

**Files:**
- Modify: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/api/PickupDtos.kt`

- [ ] **Step 1: Add fields to `CitizenPickupDetailDto`**

In `CitizenPickupDetailDto`, add after `val locationAddress: String,`:

```kotlin
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
```

(Defaults make deserialization resilient if the backend response is briefly out of sync.)

- [ ] **Step 2: Compile check**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/api/PickupDtos.kt
git commit -m "feat: add coordinates to citizen pickup detail dto"
```

---

### Task 4: Add coordinates to the `PickupDetails` model

**Files:**
- Modify: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/pickups/model/Pickup.kt`

- [ ] **Step 1: Add fields to `PickupDetails`**

In `data class PickupDetails`, add after `val lockerNumber: String,` (before `val unlockedAt: String? = null,`):

```kotlin
    val latitude: Double = 46.0569,
    val longitude: Double = 14.5058,
```

(Defaults = Ljubljana center, so any not-yet-updated construction site still yields a valid map.)

- [ ] **Step 2: Compile check**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: PASS (existing constructions still compile thanks to the defaults).

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/pickups/model/Pickup.kt
git commit -m "feat: add coordinates to PickupDetails model"
```

---

### Task 5: Map coordinates in `HttpPickupRepository`

**Files:**
- Modify: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/pickups/HttpPickupRepository.kt`

- [ ] **Step 1: Map the new fields in `toPickupDetails()`**

In `CitizenPickupDetailDto.toPickupDetails()`, add after `locationAddress = locationAddress,`:

```kotlin
        latitude = latitude,
        longitude = longitude,
```

- [ ] **Step 2: Compile check**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/pickups/HttpPickupRepository.kt
git commit -m "feat: map station coordinates from detail dto"
```

---

### Task 6: Give fakes real Ljubljana coordinates

**Files:**
- Modify: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/pickups/FakePickupRepository.kt`

- [ ] **Step 1: Add coordinates to each fake `PickupDetails`**

Add a `latitude`/`longitude` line after each `lockerNumber = ...,` in the `details` map:

- Entry `"1"` (BTC City, Šmartinska): `latitude = 46.0669, longitude = 14.5419,`
- Entry `"2"` (Kongresni trg): `latitude = 46.0498, longitude = 14.5040,`
- Entry `"3"` (Mestni trg / Magistrat): `latitude = 46.0511, longitude = 14.5065,`

Example for entry `"1"`:

```kotlin
            lockerNumber = "352",
            latitude = 46.0669,
            longitude = 14.5419,
        ),
```

- [ ] **Step 2: Compile check**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/pickups/FakePickupRepository.kt
git commit -m "test: give fake pickups real Ljubljana coordinates"
```

---

## Track C — Map component

### Task 7: Add the osmdroid dependency

**Files:**
- Modify: `ePrevzemMobile/gradle/libs.versions.toml`
- Modify: `ePrevzemMobile/composeApp/build.gradle.kts`

- [ ] **Step 1: Add version + library to the catalog**

In `gradle/libs.versions.toml`, under `[versions]` add:

```toml
osmdroid = "6.1.18"
```

Under `[libraries]` add:

```toml
osmdroid-android = { module = "org.osmdroid:osmdroid-android", version.ref = "osmdroid" }
```

- [ ] **Step 2: Reference it in androidMain**

In `composeApp/build.gradle.kts`, inside `androidMain.dependencies { ... }`, add:

```kotlin
            implementation(libs.osmdroid.android)
```

- [ ] **Step 3: Sync/resolve check**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:dependencies --configuration debugRuntimeClasspath`
Expected: PASS and `org.osmdroid:osmdroid-android:6.1.18` appears in the tree.

- [ ] **Step 4: Commit**

```bash
git add ePrevzemMobile/gradle/libs.versions.toml ePrevzemMobile/composeApp/build.gradle.kts
git commit -m "build: add osmdroid dependency for android map"
```

---

### Task 8: Declare the `EStationMap` expect component

**Files:**
- Create: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.kt`

- [ ] **Step 1: Write the expect declaration**

```kotlin
package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

/**
 * Interactive native map centered on [latitude]/[longitude] with a single marker
 * at that point. Android renders an osmdroid MapView; iOS renders an Apple
 * MapKit MKMapView. The caller controls size/shape via [modifier].
 */
@Composable
expect fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier = Modifier,
)
```

- [ ] **Step 2: Verify it fails to compile (no actuals yet)**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: FAIL — "expect ... has no actual declaration" for Android/iOS. This confirms the expect is picked up; actuals follow in Tasks 9–10. (If executing tasks strictly sequentially, proceed to 9/10 before treating this as green.)

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.kt
git commit -m "feat(ds): declare EStationMap expect component"
```

---

### Task 9: Android `actual` — osmdroid

**Files:**
- Create: `ePrevzemMobile/composeApp/src/androidMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.android.kt`
- Modify: `ePrevzemMobile/composeApp/src/androidMain/kotlin/si/mentis/eprevzemmobile/EPrevzemApp.kt`

- [ ] **Step 1: One-time osmdroid config in `EPrevzemApp.onCreate`**

In `EPrevzemApp.kt`, add imports and a config call. Result:

```kotlin
package si.mentis.eprevzemmobile

import android.app.Application
import androidx.fragment.app.FragmentActivity
import org.osmdroid.config.Configuration

class EPrevzemApp : Application() {
    override fun onCreate() {
        super.onCreate()
        AndroidAppContext.application = this
        // osmdroid needs a user agent (HTTP tile requests are rejected without one)
        // and a writable cache/config path before any MapView is created.
        Configuration.getInstance().load(
            this,
            getSharedPreferences("osmdroid", MODE_PRIVATE),
        )
        Configuration.getInstance().userAgentValue = packageName
    }
}

internal object AndroidAppContext {
    @Volatile
    var application: Application? = null

    @Volatile
    var currentActivity: FragmentActivity? = null
}
```

- [ ] **Step 2: Write the Android actual**

```kotlin
package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker

@Composable
actual fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier,
) {
    AndroidView(
        modifier = modifier,
        factory = { context ->
            MapView(context).apply {
                setTileSource(TileSourceFactory.MAPNIK)
                setMultiTouchControls(true)
                isHorizontalMapRepetitionEnabled = false
                isVerticalMapRepetitionEnabled = false
                val point = GeoPoint(latitude, longitude)
                controller.setZoom(16.0)
                controller.setCenter(point)
                overlays.add(
                    Marker(this).apply {
                        position = point
                        setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM)
                        title = label
                    },
                )
            }
        },
        update = { map ->
            val point = GeoPoint(latitude, longitude)
            map.controller.setCenter(point)
            map.overlays.filterIsInstance<Marker>().firstOrNull()?.apply {
                position = point
                title = label
            }
            map.invalidate()
        },
        onRelease = { map -> map.onDetach() },
    )
}
```

- [ ] **Step 3: Build the Android debug APK**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:assembleDebug`
Expected: PASS, BUILD SUCCESSFUL.

- [ ] **Step 4: Commit**

```bash
git add ePrevzemMobile/composeApp/src/androidMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.android.kt ePrevzemMobile/composeApp/src/androidMain/kotlin/si/mentis/eprevzemmobile/EPrevzemApp.kt
git commit -m "feat(ds): osmdroid actual for EStationMap"
```

---

### Task 10: iOS `actual` — Apple MapKit

**Files:**
- Create: `ePrevzemMobile/composeApp/src/iosMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.ios.kt`

- [ ] **Step 1: Write the iOS actual**

```kotlin
package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.interop.UIKitView
import kotlinx.cinterop.ExperimentalForeignApi
import platform.CoreLocation.CLLocationCoordinate2DMake
import platform.MapKit.MKCoordinateRegionMakeWithDistance
import platform.MapKit.MKMapView
import platform.MapKit.MKPointAnnotation

@OptIn(ExperimentalForeignApi::class)
@Composable
actual fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier,
) {
    UIKitView(
        modifier = modifier,
        factory = {
            val coordinate = CLLocationCoordinate2DMake(latitude, longitude)
            MKMapView().apply {
                addAnnotation(
                    MKPointAnnotation().apply {
                        setCoordinate(coordinate)
                        setTitle(label)
                    },
                )
                setRegion(
                    MKCoordinateRegionMakeWithDistance(coordinate, 1000.0, 1000.0),
                    animated = false,
                )
            }
        },
        update = { mapView ->
            val coordinate = CLLocationCoordinate2DMake(latitude, longitude)
            mapView.setRegion(
                MKCoordinateRegionMakeWithDistance(coordinate, 1000.0, 1000.0),
                animated = false,
            )
        },
    )
}
```

Note: `CLLocationCoordinate2DMake` and `MKCoordinateRegionMakeWithDistance` return cinterop structs by value, hence `@OptIn(ExperimentalForeignApi::class)`. If the Compose version flags `UIKitView` as experimental, also add `ExperimentalComposeUiApi::class` to the `@OptIn` (import `androidx.compose.ui.ExperimentalComposeUiApi`).

- [ ] **Step 2: Link the iOS simulator framework**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:linkDebugFrameworkIosSimulatorArm64`
Expected: PASS, BUILD SUCCESSFUL. This also resolves the expect/actual for the iOS target and confirms commonMain (Task 8) is now green.

- [ ] **Step 3: Commit**

```bash
git add ePrevzemMobile/composeApp/src/iosMain/kotlin/si/mentis/eprevzemmobile/core/designsystem/components/map/EStationMap.ios.kt
git commit -m "feat(ds): MapKit actual for EStationMap"
```

---

## Track D — Screen integration (join point: needs Task 4 + Task 8)

### Task 11: Use `EStationMap` in the Lokacija card and delete the placeholder

**Files:**
- Modify: `ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/pickups/PickupDetailsScreen.kt`

- [ ] **Step 1: Add the import**

Add to the import block:

```kotlin
import si.mentis.eprevzemmobile.core.designsystem.components.map.EStationMap
```

- [ ] **Step 2: Swap the placeholder call inside the "Lokacija" `ESummaryCard`**

Replace this line (currently first child of the Lokacija card):

```kotlin
                MapPlaceholder(locationName = state.details.locationName)
```

with:

```kotlin
                EStationMap(
                    latitude = state.details.latitude,
                    longitude = state.details.longitude,
                    label = state.details.locationName,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(160.dp)
                        .clip(EPrevzemTheme.shapes.medium),
                )
```

(`fillMaxWidth`, `height`, `clip`, `dp` are already imported in this file.)

- [ ] **Step 3: Delete the now-unused `MapPlaceholder` composable**

Remove the entire `@Composable private fun MapPlaceholder(locationName: String) { ... }` function (the grid `Canvas` block). Then remove imports that become unused **only if** no other usage remains in the file — verify each before deleting:
- `androidx.compose.foundation.Canvas`
- `androidx.compose.ui.geometry.Offset`

(Do not remove `Color`, `clip`, `background`, `CircleShape`, etc. — they are still used elsewhere in the file, e.g. `LockerChip`, `MapPlaceholder`'s siblings, `UnlockIconBubble`.)

- [ ] **Step 4: Compile check (commonMain)**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: PASS, no "unused import" hard errors (warnings are acceptable).

- [ ] **Step 5: Build Android debug to confirm full graph links**

Run (cwd `ePrevzemMobile`): `./gradlew :composeApp:assembleDebug`
Expected: PASS, BUILD SUCCESSFUL.

- [ ] **Step 6: Commit**

```bash
git add ePrevzemMobile/composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/pickups/PickupDetailsScreen.kt
git commit -m "feat: show real station map in pickup details location"
```

---

## Final verification (after all tracks merge)

- [ ] **Backend:** `dotnet build ePrevzem.sln` → PASS
- [ ] **Mobile common:** `./gradlew :composeApp:compileCommonMainKotlinMetadata` → PASS
- [ ] **Android:** `./gradlew :composeApp:assembleDebug` → PASS
- [ ] **iOS:** `./gradlew :composeApp:linkDebugFrameworkIosSimulatorArm64` → PASS
- [ ] **Manual smoke (optional):** run the app, open a pickup's "Podrobnosti prevzema", confirm the Lokacija card shows a real map with a marker at the station, and that it pans/zooms.
