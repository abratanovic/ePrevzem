# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository shape

`ePrevzem` is a multi-tenant "secure document pickup from smart lockers" prototype. The repo is a **polyglot monorepo** with three independent subprojects, each with its own toolchain. There is no top-level build that ties them together — `ePrevzem.sln` only wires the `sitrust-mock` .NET projects.

```
ePrevzem/
├── ePrevzemMobile/      # Kotlin Multiplatform / Compose Multiplatform client (Android + iOS)
└── sitrust-mock/        # SI-TRUST / eOsebna identity simulator (separate solution)
    ├── backend/         # ASP.NET Core 9 Web API (SiTrustMock + SiTrustMock.Tests)
    ├── frontend/        # React 19 + Vite 8 + Tailwind 4 web portal (SI-PASS login mock)
    └── eosebna_mobile/  # Flutter app that simulates the "eOsebna" NFC/biometric ID flow
```

The user-facing pickup client is `ePrevzemMobile` (Kotlin Multiplatform). The `sitrust-mock` subtree exists to **mock the Slovenian state identity infrastructure** (SI-TRUST / SI-PASS and eOsebna) that the real system would integrate with — its three pieces (API + web SI-PASS login + Flutter eOsebna app) cooperate to play out the identification flow against the mobile client.

Each subproject has its own README; `ePrevzemMobile/CLAUDE.md` and `ePrevzemMobile/AGENTS.md` carry the detailed design-system rules for that client and **must be followed when touching `ePrevzemMobile/`**. Treat the root README as the product overview (Slovenian).

**Per-subproject agent guides — read before editing:**

- `backend/` → `backend/AGENTS.md` (also mirrored as `backend/CLAUDE.md` / `backend/GEMINI.md`). Backend architecture, layering rules, and conventions.
- `ePrevzemMobile/` → `ePrevzemMobile/CLAUDE.md` / `ePrevzemMobile/AGENTS.md`. Compose Multiplatform design-system rules.

## Working per subproject

### `ePrevzemMobile/` — Kotlin Multiplatform (Compose)

Single Gradle module `composeApp` targeting Android + iOS (arm64, simulatorArm64). Run from `ePrevzemMobile/`:

```
./gradlew :composeApp:compileCommonMainKotlinMetadata   # fast cross-platform compile check
./gradlew :composeApp:assembleDebug                     # Android APK
./gradlew :composeApp:installDebug                      # Android install
./gradlew :composeApp:allTests
./gradlew :composeApp:testDebugUnitTest --tests "FQCN.testName"
./gradlew :composeApp:linkDebugFrameworkIosSimulatorArm64
```

On Windows shells use `gradlew.bat`. See `ePrevzemMobile/CLAUDE.md` for the design-system rules — they are non-negotiable when editing this subproject (E*-prefixed components, token-only styling, `Painter` icons from `EPrevzemIcons`, state+event split, no Android-only APIs in `commonMain`, Slovenian UI text / English code).

### `sitrust-mock/backend/` — ASP.NET Core 9 mock identity API

`net9.0`, minimal-hosting `Program.cs`, controllers in `Controllers/`, business logic in `Services/`, in-memory `Stores/`. Issues JWTs using `JwtSettings:Secret` from configuration (required — `Program.cs` throws if missing). Generates SI-PASS QR codes via `QRCoder` and exposes `/health`.

```
# from sitrust-mock/
dotnet build sitrust-mock.sln
dotnet run --project backend/SiTrustMock                 # http profile binds to a LAN IP — see launchSettings.json
dotnet test backend/SiTrustMock.Tests
dotnet test backend/SiTrustMock.Tests --filter "FullyQualifiedName~AuthAttemptStoreTests"
```

The `http` launch profile binds `applicationUrl` to a specific LAN IP (`172.20.10.9:5070`) so the Flutter `eosebna_mobile` device can reach it. If running locally without that network, switch profile to `https` or override `ASPNETCORE_URLS`. The `https` profile uses `https://localhost:7282;http://localhost:5070`.

CORS is `AllowAnyOrigin/Header/Method` because the React and Flutter clients hit it from different origins. The auth store is a `Singleton` in-memory store — state resets on restart.

### `sitrust-mock/frontend/` — React 19 + Vite 8 + Tailwind 4

Mocks the SI-PASS web login (`SiPassLoginPage`), the eOsebna companion landing (`EosebnaPage`), and a stub ePrevzem home (`ePrevzemHomePage`). Routes via `react-router-dom` v7.

```
# from sitrust-mock/frontend/
npm install
npm run dev      # vite
npm run build    # tsc -b && vite build
npm run lint     # eslint .
npm run preview
```

Tailwind v4 is wired through `@tailwindcss/vite`, not a PostCSS pipeline — no `tailwind.config.js`.

### `sitrust-mock/eosebna_mobile/` — Flutter eOsebna mock

Flutter ≥ Dart SDK `^3.11.5`. Features: QR scan (`mobile_scanner`), biometric prompt (`local_auth`), HTTP to the backend mock. Three screens: `home_screen`, `qr_scan_screen`, `success_screen`.

```
# from sitrust-mock/eosebna_mobile/
flutter pub get
flutter run                         # pick device
flutter test
flutter test test/widget_test.dart  # single file
flutter analyze
```

The app expects the SiTrustMock backend reachable at the LAN URL configured in `lib/services/` — update there when the backend host changes.

## Cross-project conventions

- **Languages:** UI copy is **Slovenian** (`ePrevzemMobile`, parts of the mocks). Code, identifiers, comments, commit messages, and docs are **English**.
- **No real PII or identity credentials.** The "SI-TRUST" and "eOsebna" flows are simulated — never wire them to real state services from this repo.
- **Subprojects are independently buildable.** Don't introduce a top-level Gradle/npm orchestrator without a strong reason; each toolchain owns its own lifecycle.
- The .NET solution at the repo root (`ePrevzem.sln`) only contains the `sitrust-mock` backend + tests. The mobile and frontend apps are intentionally outside it.