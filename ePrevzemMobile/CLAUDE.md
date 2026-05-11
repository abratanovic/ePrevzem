# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Kotlin Multiplatform / Compose Multiplatform mobile app (`si.mentis.eprevzemmobile`) targeting Android and iOS (arm64 + simulator-arm64). Single module: `composeApp`. iOS is wired up via a Kotlin framework consumed by `iosApp/iosApp.xcodeproj`.

The app is `ePrevzem` — a Slovenian "secure document pickup from smart lockers" client. UI strings are Slovenian; document everything else in English.

## Common commands

Run from repo root. Use `gradlew.bat` on Windows shells where `./gradlew` fails.

```
# Android debug build / install
./gradlew :composeApp:assembleDebug
./gradlew :composeApp:installDebug

# Multiplatform compile check (fastest "does it still compile" loop)
./gradlew :composeApp:compileCommonMainKotlinMetadata

# Tests
./gradlew :composeApp:allTests                       # all targets
./gradlew :composeApp:testDebugUnitTest              # android only
./gradlew :composeApp:testDebugUnitTest --tests "FQCN.testName"

# iOS framework (consumed by Xcode project in iosApp/)
./gradlew :composeApp:linkDebugFrameworkIosSimulatorArm64
```

There is no separate lint task wired up beyond what AGP provides (`./gradlew :composeApp:lint`).

## Architecture

### Module layout (commonMain)

```
si.mentis.eprevzemmobile
├── App.kt                          # Root @Composable; wraps WelcomeRoute in EPrevzemTheme
├── core/
│   ├── designsystem/
│   │   ├── theme/                  # Design tokens (colours, typo, spacing, radius, shapes, elevation)
│   │   ├── icons/EPrevzemIcons.kt  # Painter-returning composables — single icon vocabulary
│   │   └── components/             # buttons, cards, dialogs, feedback, inputs, layout, navigation
│   ├── navigation/                 # (placeholder)
│   └── ui/                         # (placeholder)
├── di/                             # (placeholder)
└── feature/
    └── onboarding/                 # WelcomeScreen / WelcomeRoute / WelcomeState / WelcomeEvent
```

Android-specific code lives in `composeApp/src/androidMain/` (`MainActivity` enables edge-to-edge and calls `App()`); iOS in `composeApp/src/iosMain/`.

### Design system — the load-bearing decisions

1. **Tokens flow through `EPrevzemTheme`** (`core/designsystem/theme/EPrevzemTheme.kt`). It provides six `CompositionLocal`s (`LocalEPrevzemColors`, `LocalEPrevzemTypography`, `LocalEPrevzemSpacing`, `LocalEPrevzemRadius`, `LocalEPrevzemShapes`, `LocalEPrevzemElevation`) **and** projects them into Material3's `MaterialTheme` via `toMaterial3ColorScheme()` / `toMaterial3Typography()` / `toMaterial3Shapes()`. Always read tokens via `EPrevzemTheme.colors`/`typography`/`spacing`/`shapes` — never hardcode colours, sizes, radii, or fonts.

2. **Icons are `Painter`, not `ImageVector`.** `EPrevzemIcons` exposes `@Composable fun <name>(): Painter = painterResource(Res.drawable.ic_<name>)`. Every icon ships as an **Android Vector Drawable XML** in `composeApp/src/commonMain/composeResources/drawable/`, not SVG — this Compose Multiplatform version's Android target rejects SVG at runtime (`Android platform doesn't support SVG format`). Material Symbols icons (`viewBox="0 -960 960 960"`) must be wrapped in `<group android:translateY="960">` because Android Vector Drawables have no viewport offset. Public component APIs take `Painter` parameters (never `ImageVector`); callers pass `EPrevzemIcons.<name>()`.

3. **Component naming.** Every public design-system composable is prefixed `E` (e.g. `EPrimaryButton`, `EInfoCard`, `EStatusChip`, `ETopBar`, `EScaffold`). `EScaffold` is a hand-rolled scaffold (not Material3's) — it paints the background, slots `topBar` / `bottomBar`, and applies `navigationBarsPadding` to the bottom slot only. The top bar is responsible for its own `statusBarsPadding` so its background extends behind the status bar.

4. **Feature screens follow a state + event split.** See `feature/onboarding/`: `WelcomeState` (data), `WelcomeEvent` (sealed), `WelcomeScreen` (stateless, takes `state` + `onEvent`), `WelcomeRoute` (stateful entry, holds `remember { mutableStateOf(...) }`). No ViewModel layer exists yet — `*Route` composables own state until one is introduced. Keep `*Screen` composables pure.

### Resources & generated code

- Drawables and fonts live in `composeApp/src/commonMain/composeResources/{drawable,font}/`. The Compose Resources plugin generates `eprevzemmobile.composeapp.generated.resources.Res.{drawable,font}.<name>` accessors — filenames must be `lowercase_with_underscores`. After adding a file, run any Gradle task once to regenerate, then import the accessor.
- For variable fonts, register the same `Res.font.<file>` under each `FontWeight` you need; the renderer interpolates the `wght` axis. The font factory must come from `org.jetbrains.compose.resources`, not `androidx.compose.ui.text.font`.

### Edge-to-edge

`MainActivity.onCreate` calls `enableEdgeToEdge()` before `super.onCreate`. Insets are not applied globally — each surface handles its own (`ETopBar` → `statusBarsPadding`, `EScaffold`'s bottom slot → `navigationBarsPadding`). If a screen looks like the system bars are floating on a white strip, the screen is missing its inset modifier — don't reintroduce a global `safeContentPadding()` on `EScaffold`.

## Working in this repo

- The Kotlin metadata compile (`compileCommonMainKotlinMetadata`) catches most cross-platform compile errors quickly without doing a full Android assembly.
- Compose Multiplatform version pin is in `gradle/libs.versions.toml` (`composeMultiplatform`, `material3`); the resources artifact tracks the same version via `compose-components-resources`.
- `local.properties` holds the Android SDK path and is git-ignored.

## Non-negotiable rules

- Always use the ePrevzem design system in feature UI. Do not use raw Material3 components directly if an `E*` component exists.
- Do not hardcode colours, typography, spacing, padding, radius, elevation, icon sizes, or fonts in feature screens.
- Public reusable UI components must live in `core/designsystem/components` and must be prefixed with `E`.
- Feature screens must stay stateless: render `State`, emit `Event`. Put temporary state in `*Route` until ViewModels are introduced.
- Do not put business logic, API calls, storage access, or navigation decisions inside `*Screen` composables.
- Do not use Android-only APIs in `commonMain`.
- Do not expose DTOs, tokens, registration/session tokens, or sensitive locker data in UI.
- Use `Painter` icons from `EPrevzemIcons`; do not introduce `ImageVector` icon APIs.
- Keep Slovenian text in UI; write code comments, docs, and internal names in English.
- Never commit to git without permission
- Never add Co-authored by Claude to git commit messages and keep git commit messages short