# Station Map — Fullscreen + Custom Marker (short plan)

**Goal:** Tapping a corner expand button on the inline station map opens a fullscreen map with full controls; the marker reproduces the old `MapPlaceholder` look (primary circle + white `ic_location` pin) on both platforms.

**Approach:** Split `EStationMap` into a common wrapper + an `internal expect PlatformStationMap`. The wrapper renders the marker once (offscreen `CanvasDrawScope` from the real `ic_location` painter → `ImageBitmap`), shows the inline map + expand button, and a fullscreen `Dialog`. Each platform converts the `ImageBitmap` to its native marker.

---

### Task 1: Add the fullscreen icon
- Create `composeApp/src/commonMain/composeResources/drawable/ic_fullscreen.xml` (Material Symbols fullscreen, `<group translateY=960>` wrapped).
- Add `ic_fullscreen` import + `@Composable fun fullscreen(): Painter` to `EPrevzemIcons.kt`.
- Verify: `./gradlew :composeApp:compileCommonMainKotlinMetadata`.

### Task 2: Common wrapper + marker render + fullscreen Dialog
- Rewrite `core/designsystem/components/map/EStationMap.kt`:
  - `internal expect @Composable fun PlatformStationMap(latitude, longitude, label, markerIcon: ImageBitmap, showZoomControls: Boolean, modifier: Modifier)`.
  - `rememberStationMarker(): ImageBitmap` — 44.dp primary circle + 22.dp white `ic_location`, drawn via `CanvasDrawScope`.
  - public `EStationMap(latitude, longitude, label, modifier)` — Box(inline `PlatformStationMap(showZoomControls=false)` + expand button top-end); `var fullscreen`; when true a `Dialog(usePlatformDefaultWidth=false)` with fullscreen `PlatformStationMap(showZoomControls=true)` + close button.

### Task 3: Android actual
- Rewrite `EStationMap.android.kt`: `actual fun PlatformStationMap` — osmdroid `MapView`, `marker.icon = BitmapDrawable(resources, markerIcon.asAndroidBitmap())`, anchor center-center, `zoomController` shown when `showZoomControls`.

### Task 4: iOS actual
- Rewrite `EStationMap.ios.kt`: `actual fun PlatformStationMap` — MapKit; convert `markerIcon` → PNG via Skia → `NSData` → `UIImage(scale = screen scale)` → `MKAnnotationView.image` via delegate.

### Final verification
- `./gradlew :composeApp:compileCommonMainKotlinMetadata` → PASS
- `./gradlew :composeApp:assembleDebug` → PASS
- `./gradlew :composeApp:linkDebugFrameworkIosSimulatorArm64` → PASS
