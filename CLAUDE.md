# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository shape

`ePrevzem` is a multi-tenant "secure document pickup from smart lockers" platform. The repo is a **polyglot monorepo** with several independent subprojects, each with its own toolchain. There is no top-level build that ties them all together — `ePrevzem.sln` only wires the `backend/` projects.

```
ePrevzem/
├── backend/             # ASP.NET Core 9 modular monolith — the real production backend
│   ├── ePrevzem.Api             # thin controllers, DI, auth, OpenAPI
│   ├── ePrevzem.Application     # MediatR use cases, DTOs, validators, ports
│   ├── ePrevzem.Domain          # aggregates, value objects, domain events (zero deps)
│   ├── ePrevzem.Infrastructure  # EF Core (Npgsql), adapters, SystemClock
│   └── ePrevzem.Tests           # xUnit + Testcontainers Postgres + WebApplicationFactory
├── ePrevzemMobile/      # Kotlin Multiplatform / Compose Multiplatform client (Android + iOS)
└── sitrust-mock/        # SI-TRUST / eOsebna identity simulator (separate solution)
    ├── backend/         # ASP.NET Core 9 Web API (SiTrustMock + SiTrustMock.Tests)
    ├── frontend/        # React 19 + Vite 8 + Tailwind 4 SI-PASS web login mock
    └── eosebna_mobile/  # Flutter app simulating the eOsebna NFC/biometric ID flow
```

The user-facing pickup client is `ePrevzemMobile`. The **real backend** is `backend/` (Clean Architecture). The `sitrust-mock` subtree is a **separate solution** that simulates Slovenian state identity infrastructure (SI-TRUST / SI-PASS and eOsebna) so the mobile client can play out the identification flow end-to-end — it is not part of the production system.

**Per-subproject agent guides — read before editing:**

- `backend/` → `backend/AGENTS.md` (mirrored as `backend/CLAUDE.md` / `backend/GEMINI.md`). Authoritative for backend architecture, layering rules, and conventions.
- `ePrevzemMobile/` → `ePrevzemMobile/CLAUDE.md` / `ePrevzemMobile/AGENTS.md`. Compose Multiplatform design-system rules.

## Working per subproject

### `backend/` — ASP.NET Core 9 modular monolith (Clean Architecture)

`net9.0`, EF Core 9 + Npgsql (PostgreSQL), MediatR, FluentValidation, Serilog, JWT bearer auth. Strict one-way dependency flow: `Api → Application → Domain` and `Infrastructure → Application, Domain`. Feature modules (`Organizations / Pickups / Lockers / Delegations / Identity / Audit / Notifications`) mirror their folder layout across `Domain/` and `Application/`. Cross-module communication happens through domain events, never direct entity references. See `backend/AGENTS.md` for the full ruleset — the dependency rule, thin-controller rule, multi-tenancy via `ITenantContext` global filters, append-only audit log, and aggregate-encapsulated state transitions are non-negotiable.

```
# from repo root
dotnet build ePrevzem.sln
dotnet run --project backend/ePrevzem.Api
dotnet test backend/ePrevzem.Tests
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Pickups"

# EF migrations
dotnet ef migrations add <Name> --project backend/ePrevzem.Infrastructure --startup-project backend/ePrevzem.Api
```

Integration tests use Testcontainers Postgres + `WebApplicationFactory` — **never mock the DB**.

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

`net9.0`, minimal-hosting `Program.cs`, controllers in `Controllers/`, business logic in `Services/`, in-memory `Stores/`. Issues JWTs using `JwtSettings:Secret` from configuration (required — `Program.cs` throws if missing). Generates SI-PASS QR codes via `QRCoder` and exposes `/health`. This is a separate solution (`sitrust-mock/sitrust-mock.sln`) — not part of `ePrevzem.sln`.

```
# from sitrust-mock/
dotnet build sitrust-mock.sln
dotnet run --project backend/SiTrustMock                 # http profile binds to a LAN IP — see launchSettings.json
dotnet test backend/SiTrustMock.Tests
dotnet test backend/SiTrustMock.Tests --filter "FullyQualifiedName~AuthAttemptStoreTests"
```

The `http` launch profile binds `applicationUrl` to a specific LAN IP (`172.20.10.9:5070`) so the Flutter `eosebna_mobile` device can reach it. If running locally without that network, switch profile to `https` or override `ASPNETCORE_URLS`. The `https` profile uses `https://localhost:7282;http://localhost:5070`. CORS is `AllowAnyOrigin/Header/Method` because the React and Flutter clients hit it from different origins. The auth store is a `Singleton` in-memory store — state resets on restart.

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

- **Languages:** UI copy is **Slovenian** (`ePrevzemMobile`, parts of the mocks, user-facing backend error messages). Code, identifiers, comments, commit messages, and docs are **English**.
- **No real PII or identity credentials.** The "SI-TRUST" and "eOsebna" flows are simulated — never wire them to real state services from this repo.
- **Subprojects are independently buildable.** Don't introduce a top-level Gradle/npm orchestrator without a strong reason; each toolchain owns its own lifecycle.
- The .NET solution at the repo root (`ePrevzem.sln`) contains **only** the `backend/` Clean Architecture projects. The mobile, React frontend, Flutter mock, and `sitrust-mock` backend are intentionally outside it.

## GIT Conventions
- short git messages
- never include "Co-authored by <AI AGENT NAME>" messages in git commits
- never mention any AI agents in commits