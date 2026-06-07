# Real interactive map in the pickup Location section

**Date:** 2026-06-07
**Status:** Approved (design)
**Scope:** `ePrevzemMobile/` + `backend/`

## Problem

In the "Podrobnosti prevzema" (pickup details) screen, the "Lokacija" section
renders `MapPlaceholder` — a hand-drawn grid `Canvas` with a centered pin that
is **not** tied to any real position. It should show an actual interactive map
with a marker at the real package-station location.

## Key facts established during brainstorming

- The domain `Location` value object (`backend/ePrevzem.Domain/Lockers/Location.cs`)
  already holds `Latitude` and `Longitude` (`decimal`). They are **not** exposed
  past the domain — the citizen pickup read projection only surfaces the postal
  address (`Address`, `HouseNumber`, `ZipCode`, `City`).
- Compose Multiplatform has no built-in map; Android and iOS need different
  native engines.

## Decisions

1. **Map type:** interactive native map (pan/zoom), not a static image.
2. **Coordinate source:** the existing `Location.Latitude/Longitude` on the
   station claim — no geocoding, no new data entry.
3. **Android engine:** OpenStreetMap via **osmdroid** (free, no API key, no
   billing). iOS uses Apple **MapKit** (no key). Both OSM-style, visually
   consistent.
4. **Size:** keep the map at ~160.dp height inside the existing card.
5. **Placeholder:** delete `MapPlaceholder` and its grid/Canvas code entirely.

## Design

### 1. Data plumbing — surface coordinates that already exist

**Backend (`backend/`):**

- `CitizenPickupRow` (private class in `ePrevzem.Infrastructure/Pickups/PickupReadRepository.cs`):
  add `Latitude` and `Longitude` (`decimal`); populate in `CitizenPickupQuery()`
  from `claim.Location.Latitude` / `claim.Location.Longitude`.
- `CitizenPickupDetailResponse` (`ePrevzem.Application/Pickups/Dtos/CitizenPickupResponses.cs`):
  add `Latitude` and `Longitude` (`decimal`); set them in
  `GetCitizenPickupDetailAsync` mapping.
- The list `CitizenPickupResponse` is left untouched — only the detail screen
  shows a map.

**Mobile (`ePrevzemMobile/`):**

- `CitizenPickupDetailDto` (`data/api/PickupDtos.kt`): add `latitude: Double`,
  `longitude: Double`.
- `PickupDetails` model (`feature/pickups/model/Pickup.kt`): add
  `latitude: Double`, `longitude: Double`.
- `HttpPickupRepository.toPickupDetails()`: map the new fields.
- `placeholderDetails()` (in `PickupDetailsScreen.kt`) and `FakePickupRepository`:
  default to a real Ljubljana coordinate so previews/fakes render a valid map.

### 2. The map component — `EStationMap` (expect/actual)

New design-system component, following the E-prefix and token rules:

- `core/designsystem/components/map/EStationMap.kt` (commonMain):
  `expect @Composable fun EStationMap(latitude: Double, longitude: Double, label: String, modifier: Modifier = Modifier)`.
  Caller applies a fixed 160.dp height, clipped to `EPrevzemTheme.shapes.medium`.
- `androidMain`: `actual` wraps an osmdroid `MapView` in `AndroidView`, centered
  on the coordinate with a marker. osmdroid dependency added to the `composeApp`
  Android source set; one-time osmdroid config (user-agent) in `MainActivity`.
- `iosMain`: `actual` wraps `MKMapView` (Apple MapKit) in `UIKitView`, centered
  with an `MKPointAnnotation`.
- Rotation/clutter off; pan and zoom on — a clean but genuinely interactive
  location preview.

### 3. Screen integration

In `PickupDetailsScreen.kt`, inside the "Lokacija" `ESummaryCard`, replace the
`MapPlaceholder(...)` call with
`EStationMap(state.details.latitude, state.details.longitude, state.details.locationName, modifier = Modifier.fillMaxWidth().height(160.dp))`.
The `locationName` / `locationAddress` `Text`s below stay. Delete the
`MapPlaceholder` composable and its now-unused Canvas/grid imports.

### 4. Testing & verification

- **Backend:** extend the citizen pickup detail test to assert latitude /
  longitude are returned (Testcontainers Postgres + `WebApplicationFactory`,
  never mock the DB).
- **Mobile:** `compileCommonMainKotlinMetadata`, `assembleDebug` (Android /
  osmdroid), and `linkDebugFrameworkIosSimulatorArm64` (iOS / MapKit) to confirm
  both actuals compile. No unit test for the native view itself.

## Trade-offs

- `EStationMap` is the first native-UI interop in this design system. Its
  `expect` lives in commonMain (rule-compliant); platform APIs live only in
  `androidMain` / `iosMain`, which the "no Android-only APIs in commonMain" rule
  permits.
- osmdroid (vs Google Maps) trades a little polish for zero external setup —
  the right call for a mock/course-stage project.
