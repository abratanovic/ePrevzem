# Identity (System Admin v1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship username + password authentication for the SystemAdmin role with rotating refresh tokens and a config-seeded bootstrap admin, following Clean Architecture per `backend/AGENTS.md`.

**Architecture:** Feature module `Identity` across the four backend projects. Domain owns `SystemAdmin` (extended) and `RefreshToken` (new) aggregates plus events; Application defines `IPasswordHasher` and `ITokenService` ports and MediatR use cases; Infrastructure provides EF mappings, `PasswordHasher<SystemAdmin>` adapter, JWT token service, and a hosted seeder; Api exposes two anonymous endpoints. ASP.NET Identity is intentionally not used.

**Tech Stack:** ASP.NET Core 9, EF Core 9 + Npgsql, MediatR, FluentValidation, `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (standalone), JWT bearer (already wired), Testcontainers Postgres + xUnit + FluentAssertions.

**Reference spec:** `docs/superpowers/specs/2026-05-18-identity-system-admin-design.md`. The spec is authoritative when this plan and the spec disagree.

---

## How to work this plan

- Read the spec section relevant to each task before you start it.
- Read `backend/AGENTS.md` once before starting. The dependency rule, thin-controller rule, append-only audit, and aggregate-encapsulated state transitions are hard constraints.
- Before adding a port, type, or pattern, **look at how the codebase already does it** (e.g. existing aggregates under `Domain/Pickups`, existing ports under `Application/Common/Abstractions`, existing EF configs under `Infrastructure/Persistence`). Match those conventions — naming, folder layout, base classes, equality, immutability. Do not invent a new style.
- **Ask, don't guess.** Whenever you hit an architectural choice not covered here (e.g. how `Result<T>` is shaped if it exists, how domain events are dispatched, how validators are registered, how migrations are named/sequenced, whether `OrganizationId` filters need anything special for identity tables), stop and ask. Do not improvise.
- TDD where it pays: aggregates and handlers get tests first. Wiring code (DI registration, controller plumbing) does not need a failing test first — verify by running the integration tests at the end of the task.
- Commit at the end of each task with a focused message. Do not bundle tasks.
- After each task: run `dotnet build ePrevzem.sln` and the relevant tests. Don't move on with a red build.

---

## File map

**Domain/Identity/**
- `SystemAdmin.cs` (modify) — add `PasswordHash`, `LastLoginAt`, `SetPassword`, `RecordLogin`; update `Create` factory.
- `RefreshToken.cs` (new) — sibling aggregate.
- `RefreshTokenId.cs` (new) — strongly-typed id, match style of existing `*Id` types in `Domain/Identity`.
- `Events/SystemAdminPasswordChanged.cs` (new)
- `Events/SystemAdminLoggedIn.cs` (new)
- `Events/SystemAdminLoginFailed.cs` (new)
- `Events/RefreshTokenRotated.cs` (new)
- `Events/RefreshTokenChainRevoked.cs` (new)

**Application/Common/Abstractions/**
- `IPasswordHasher.cs` (new)
- `ITokenService.cs` (new)
- `IEPrevzemDbContext.cs` (modify) — add `DbSet<SystemAdmin>`, `DbSet<RefreshToken>`.

**Application/Identity/**
- `Login/LoginAdminCommand.cs` (new) — command + handler.
- `Login/LoginAdminValidator.cs` (new)
- `Refresh/RefreshAdminTokenCommand.cs` (new) — command + handler.
- `Refresh/RefreshAdminTokenValidator.cs` (new)
- `Dtos/AdminTokenResponse.cs` (new)

**Infrastructure/Identity/**
- `PasswordHasherAdapter.cs` (new)
- `JwtTokenService.cs` (new)
- `IdentitySeeder.cs` (new, `IHostedService`)
- `Persistence/SystemAdminConfiguration.cs` (new)
- `Persistence/RefreshTokenConfiguration.cs` (new)

**Infrastructure/**
- `Persistence/EPrevzemDbContext.cs` (modify) — add `DbSet`s; keep plain `DbContext`.
- `DependencyInjection.cs` (modify) — register adapters and hosted service; bind options.
- `Persistence/Migrations/<ts>_Identity_SystemAdminAndRefreshTokens.*` (generated).

**Api/**
- `Controllers/Admin/AdminAuthController.cs` (new)
- `Configuration/IdentityOptions.cs` (new)
- `Authentication/HttpCurrentUser.cs` (new)
- `Program.cs` (modify) — bind `IdentityOptions`, register `IHttpContextAccessor` + `ICurrentUser`, ensure role-aware authorization.
- `appsettings.json` (modify) — empty `Identity` section.
- `appsettings.Development.json` (modify) — dev `Username` only.

**ePrevzem.Tests/Identity/**
- Unit tests for `SystemAdmin` and `RefreshToken`.
- Integration tests for `/login`, `/refresh`, seeder.

---

## Task 1: Domain — RefreshToken aggregate and id

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/RefreshTokenId.cs`
- Create: `backend/ePrevzem.Domain/Identity/RefreshToken.cs`
- Test: `backend/ePrevzem.Tests/Identity/Domain/RefreshTokenTests.cs`

**Intent:** Sibling aggregate to `SystemAdmin`. References `SystemAdmin` by id only. Pure C# — no EF, no MediatR.

- [ ] **Step 1: Inspect existing `*Id` types**

Read `backend/ePrevzem.Domain/Identity/SystemAdminId.cs` and one or two other `*Id` types under `Domain/` to see the established style (record struct vs. class, equality, constructors). Mirror it. **If the project uses a base type or pattern you cannot infer with confidence, ask.**

- [ ] **Step 2: Write failing tests for `RefreshToken`**

Cover:
- `Issue` creates a token with `RevokedAt == null`, `ReplacedByTokenId == null`, expected `ExpiresAt`.
- `Rotate(replacementId, now)` sets `RevokedAt = now` and `ReplacedByTokenId = replacementId`, raises `RefreshTokenRotated`.
- `Revoke(now)` sets `RevokedAt = now`; calling `Revoke` twice is a no-op (or throws — decide by looking at how other aggregates handle idempotency in this codebase; **if unclear, ask**).
- `IsActive(now)` returns false when revoked, false when expired, true otherwise.

Use xUnit + FluentAssertions to match the test project's style. Read one existing aggregate test (e.g. for `Package` or `Delegation`) to align structure.

- [ ] **Step 3: Run tests to verify failure**

```
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~RefreshTokenTests"
```

Expected: compilation failure (type doesn't exist).

- [ ] **Step 4: Implement `RefreshTokenId` and `RefreshToken`**

`RefreshTokenId` matches the existing style.

`RefreshToken : AggregateRoot<RefreshTokenId>` with the fields from the spec (`SystemAdminId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt?`, `ReplacedByTokenId?`). Private constructor for EF. Static `Issue(...)` factory. Guarded `Rotate` and `Revoke`. `IsActive(DateTimeOffset now)` query method. Raise events via the existing `AggregateRoot` event mechanism — copy the pattern from another aggregate.

- [ ] **Step 5: Add the two events**

`Events/RefreshTokenRotated.cs` and `Events/RefreshTokenChainRevoked.cs`. Match the shape of existing events in `Domain/Identity/Events/` (you committed `EmployeeAccount*` events recently — use those as the template).

- [ ] **Step 6: Run tests to verify pass**

```
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~RefreshTokenTests"
```

Expected: all green.

- [ ] **Step 7: Commit**

```
git add backend/ePrevzem.Domain/Identity/RefreshToken*.cs backend/ePrevzem.Domain/Identity/Events/RefreshToken*.cs backend/ePrevzem.Tests/Identity/Domain/RefreshTokenTests.cs
git commit -m "feat(domain): add RefreshToken aggregate for admin auth"
```

---

## Task 2: Domain — extend `SystemAdmin`

**Files:**
- Modify: `backend/ePrevzem.Domain/Identity/SystemAdmin.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/SystemAdminPasswordChanged.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/SystemAdminLoggedIn.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/SystemAdminLoginFailed.cs`
- Test: `backend/ePrevzem.Tests/Identity/Domain/SystemAdminTests.cs`

**Intent:** Add `PasswordHash`, `LastLoginAt`, and guarded mutations. `Create` factory now requires a hash.

- [ ] **Step 1: Write failing tests**

Cover:
- `Create(id, username, passwordHash, now)` rejects empty username and empty hash; trims and lowercases username; sets `CreatedAt = now`, `LastLoginAt = null`.
- `SetPassword(newHash, now)` updates the hash and raises `SystemAdminPasswordChanged`. Empty hash → throw.
- `RecordLogin(now)` updates `LastLoginAt` and raises `SystemAdminLoggedIn`.
- Username normalization is consistent (caller-side concern vs. aggregate-side concern — **if the convention isn't obvious from other aggregates, ask**; default to aggregate-side normalization for v1).

- [ ] **Step 2: Run tests to verify failure**

```
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~SystemAdminTests"
```

- [ ] **Step 3: Extend `SystemAdmin`**

Add `PasswordHash` and `LastLoginAt`. Update `Create` to take and validate `passwordHash`. Add `SetPassword` and `RecordLogin`. Add the three event files. `SystemAdminLoginFailed(string attemptedUsername, DateTimeOffset at)` — raised by the handler, not by the aggregate (it's about an attempted, non-existing or wrong-credential admin).

Decide where `SystemAdminLoginFailed` lives — it is a domain event but not aggregate-bound. Look at how `Pickup`/`Delegation` raise non-aggregate events, if any; **if there is no precedent, ask** whether to put it under `Domain/Identity/Events` and dispatch it from the handler, or to define it in `Application/Identity` instead.

- [ ] **Step 4: Run tests to verify pass**

- [ ] **Step 5: Commit**

```
git commit -m "feat(domain): SystemAdmin holds password hash and login state"
```

---

## Task 3: Application — ports

**Files:**
- Create: `backend/ePrevzem.Application/Common/Abstractions/IPasswordHasher.cs`
- Create: `backend/ePrevzem.Application/Common/Abstractions/ITokenService.cs`
- Modify: `backend/ePrevzem.Application/Common/Abstractions/IEPrevzemDbContext.cs`

**Intent:** Define the two new ports the handlers will depend on, and expose the new `DbSet`s on the context interface. **No Identity types, no EF types, no ASP.NET types in Application.**

- [ ] **Step 1: Read existing ports**

Look at `IClock`, `ICurrentUser`, and `IEPrevzemDbContext` to match the naming and namespace style.

- [ ] **Step 2: Add `IPasswordHasher`**

```csharp
namespace ePrevzem.Application.Common.Abstractions;

public interface IPasswordHasher
{
    string Hash(string plaintext);
    PasswordVerification Verify(string hash, string plaintext);
}

public enum PasswordVerification { Failed, Success, NeedsRehash }
```

- [ ] **Step 3: Add `ITokenService`**

```csharp
using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ITokenService
{
    AccessTokenResult IssueAccessToken(SystemAdmin admin);
    RefreshTokenResult IssueRefreshToken(DateTimeOffset now);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenResult(string Plaintext, string Hash, DateTimeOffset ExpiresAt);
```

If Application/Domain reference rules disallow `SystemAdmin` here (they shouldn't — Application depends on Domain), keep this as-is. **If you find a reason this leaks abstraction, ask before changing.**

- [ ] **Step 4: Extend `IEPrevzemDbContext`**

Add `DbSet<SystemAdmin> SystemAdmins { get; }` and `DbSet<RefreshToken> RefreshTokens { get; }`. Look at the file first — if it uses a different abstraction (`IQueryable<T>` properties, repository pattern), match it. **If unclear, ask.**

- [ ] **Step 5: Build**

```
dotnet build ePrevzem.sln
```

Expected: green (no implementations yet; only the interface change to `EPrevzemDbContext` happens in Task 6 — the build may fail here on `EPrevzemDbContext` not implementing the new members; if so, add the `DbSet` properties to `EPrevzemDbContext` now as a temporary build fix, with no configurations yet).

- [ ] **Step 6: Commit**

```
git commit -m "feat(application): add IPasswordHasher and ITokenService ports"
```

---

## Task 4: Application — `LoginAdminCommand`

**Files:**
- Create: `backend/ePrevzem.Application/Identity/Dtos/AdminTokenResponse.cs`
- Create: `backend/ePrevzem.Application/Identity/Login/LoginAdminCommand.cs`
- Create: `backend/ePrevzem.Application/Identity/Login/LoginAdminValidator.cs`
- Test: `backend/ePrevzem.Tests/Identity/Application/LoginAdminHandlerTests.cs`

**Intent:** Use case implementing the login flow from spec §"Use cases".

- [ ] **Step 1: Inspect an existing handler**

Pick one in `Application/` (e.g. a Pickups or Delegations handler). Match return type (does the project use `Result<T>`, `ErrorOr<T>`, exceptions, or direct return?), constructor injection style, and validator wiring. **If you can't find a clear pattern, ask which to use.**

- [ ] **Step 2: Add the DTO**

`AdminTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt)`.

- [ ] **Step 3: Write the validator**

`LoginAdminValidator` with `Username` not empty / ≤128 chars, `Password` not empty / ≤256 chars. Match existing validator file style.

- [ ] **Step 4: Write failing handler tests**

Use in-memory fakes for `IEPrevzemDbContext`, `IClock`, `IPasswordHasher`, `ITokenService`. Cover:
- Unknown username → returns invalid-credentials failure; `IPasswordHasher.Verify` was still called (timing equalization against a fixed dummy hash); a `SystemAdminLoginFailed` event was raised (via whatever event-collection mechanism the codebase uses).
- Wrong password → invalid-credentials; `SystemAdminLoginFailed` raised.
- Correct password, `Verify` returns `Success` → returns tokens, `LastLoginAt` updated, a `RefreshToken` row persisted, `SystemAdminLoggedIn` raised.
- Correct password, `Verify` returns `NeedsRehash` → `PasswordHash` is rotated to the new hash before saving.

Watch out for: don't test the JWT contents here — that's `JwtTokenService`'s job. The handler test treats `ITokenService` as a fake.

- [ ] **Step 5: Implement the handler**

Behaviour per spec §"LoginAdminCommand". Steps in order:
1. Normalize username.
2. Load admin (single query, no tracking issues — match how other handlers read aggregates).
3. If absent: call `Verify` against a constant fake hash (define a `private const string FakeHashForTiming` — generate it once at startup or hardcode a known-format PBKDF2 string), raise `SystemAdminLoginFailed`, return failure.
4. Else `Verify(admin.PasswordHash, password)`. Map results per spec.
5. On `NeedsRehash`, hash plaintext again, call `admin.SetPassword(newHash, now)`.
6. `admin.RecordLogin(now)`.
7. `ITokenService.IssueAccessToken(admin)` and `IssueRefreshToken(now)`.
8. Persist `RefreshToken.Issue(...)`.
9. `SaveChangesAsync`. Return DTO.

For the failure response shape, **match the project's convention** (ProblemDetails type/code or `Result<T>` failure). The spec gives type `urn:eprevzem:identity:invalid-credentials` and the Slovenian detail string — use those.

- [ ] **Step 6: Run tests to verify pass**

```
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~LoginAdminHandlerTests"
```

- [ ] **Step 7: Commit**

```
git commit -m "feat(application): add LoginAdminCommand handler"
```

---

## Task 5: Application — `RefreshAdminTokenCommand`

**Files:**
- Create: `backend/ePrevzem.Application/Identity/Refresh/RefreshAdminTokenCommand.cs`
- Create: `backend/ePrevzem.Application/Identity/Refresh/RefreshAdminTokenValidator.cs`
- Test: `backend/ePrevzem.Tests/Identity/Application/RefreshAdminTokenHandlerTests.cs`

**Intent:** Rotation with reuse detection per spec §"RefreshAdminTokenCommand".

- [ ] **Step 1: Write failing tests**

Cover:
- Unknown token hash → invalid-refresh-token failure.
- Revoked token presented → walks `ReplacedByTokenId` chain forward and revokes each still-active descendant; raises `RefreshTokenChainRevoked`; returns failure.
- Expired token → failure; no rotation.
- Happy path → new pair issued; old token has `RevokedAt = now` and `ReplacedByTokenId = newId`; `RefreshTokenRotated` raised.

- [ ] **Step 2: Implement validator**

`RefreshToken` not empty.

- [ ] **Step 3: Implement the handler**

1. SHA-256 the incoming plaintext; base64-encode to match storage form.
2. Lookup by `TokenHash`.
3. Branch per spec.
4. For the chain-revocation walk: load tokens lazily via repeated lookups by id, or in one batched query if the codebase already favours that — **if unclear, pick the lazy walk for v1 (chains are short) and note it in the commit message.**
5. On happy path: issue new pair, call `oldToken.Rotate(newId, now)`, persist new token, save.

- [ ] **Step 4: Run tests to verify pass**

- [ ] **Step 5: Commit**

```
git commit -m "feat(application): add RefreshAdminTokenCommand with rotation and reuse detection"
```

---

## Task 6: Infrastructure — EF configurations and DbContext

**Files:**
- Create: `backend/ePrevzem.Infrastructure/Identity/Persistence/SystemAdminConfiguration.cs`
- Create: `backend/ePrevzem.Infrastructure/Identity/Persistence/RefreshTokenConfiguration.cs`
- Modify: `backend/ePrevzem.Infrastructure/Persistence/EPrevzemDbContext.cs`

**Intent:** Map both aggregates. `EPrevzemDbContext` remains a plain `DbContext` (no `IdentityDbContext`).

- [ ] **Step 1: Inspect an existing configuration**

Look at the EF configuration for another aggregate already in the codebase (Pickups, Lockers, Delegations — whichever has one). Match conventions: table naming (snake_case vs. PascalCase, plural vs. singular), how strongly-typed ids are converted, owned types, indexes, FKs. **If multiple styles exist, ask.**

- [ ] **Step 2: Write `SystemAdminConfiguration`**

- Table per existing convention (spec says `system_admins`; match what the codebase already does).
- Strongly-typed-id conversion for `SystemAdminId`.
- `Username` `text NOT NULL`, unique index.
- `PasswordHash` `text NOT NULL`.
- `CreatedAt`, `LastLoginAt` as `timestamptz` (Npgsql default for `DateTimeOffset`).

- [ ] **Step 3: Write `RefreshTokenConfiguration`**

- Strongly-typed-id conversions for `RefreshTokenId`, `SystemAdminId`, `ReplacedByTokenId?`.
- `TokenHash` `text NOT NULL`, unique index.
- Composite index on `(SystemAdminId, RevokedAt)`.
- FK `SystemAdminId → system_admins(Id) ON DELETE CASCADE`.

- [ ] **Step 4: Confirm `DbSet`s exist on the context**

If you added them temporarily in Task 3, leave them. Otherwise add `DbSet<SystemAdmin> SystemAdmins` and `DbSet<RefreshToken> RefreshTokens`. Configurations are picked up by the existing `ApplyConfigurationsFromAssembly`.

- [ ] **Step 5: Build**

```
dotnet build ePrevzem.sln
```

- [ ] **Step 6: Commit**

```
git commit -m "feat(infra): EF mappings for SystemAdmin and RefreshToken"
```

---

## Task 7: Infrastructure — `PasswordHasherAdapter`

**Files:**
- Create: `backend/ePrevzem.Infrastructure/Identity/PasswordHasherAdapter.cs`
- Test: `backend/ePrevzem.Tests/Identity/Infrastructure/PasswordHasherAdapterTests.cs`

**Intent:** Wrap `Microsoft.AspNetCore.Identity.PasswordHasher<SystemAdmin>`. Add the NuGet reference for `Microsoft.AspNetCore.Identity` (the package containing `PasswordHasher<T>` — verify the exact package name; in .NET 9 it's `Microsoft.AspNetCore.Identity` or its `.Core` variant) **to `ePrevzem.Infrastructure` only**. Application must not gain this reference.

- [ ] **Step 1: Add package reference**

Verify the package name and add to `ePrevzem.Infrastructure.csproj`. **If you're unsure which package to add, ask.**

- [ ] **Step 2: Write failing tests**

- `Hash` returns a non-empty string different from the input.
- `Verify(hash, originalPlaintext)` returns `Success`.
- `Verify(hash, wrongPlaintext)` returns `Failed`.
- `NeedsRehash` mapping: this is hard to provoke deterministically without a stale hash. Skip the explicit test — the integration test in Task 12 can cover it indirectly, or use a stored-known-format old hash if you can construct one. **If unclear, ask.**

- [ ] **Step 3: Implement**

```csharp
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<SystemAdmin> _inner = new();
    private static readonly SystemAdmin Dummy = /* a reusable instance — PasswordHasher<T> doesn't use the user; pass any */;

    public string Hash(string plaintext) => _inner.HashPassword(Dummy, plaintext);

    public PasswordVerification Verify(string hash, string plaintext) =>
        _inner.VerifyHashedPassword(Dummy, hash, plaintext) switch
        {
            PasswordVerificationResult.Success            => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.NeedsRehash,
            _                                              => PasswordVerification.Failed,
        };
}
```

Constructing `Dummy` requires a `SystemAdmin` instance — the aggregate's factory now requires a hash, so the dummy needs one too. Use a tiny constant pre-hashed value (chicken-and-egg: hash a literal once and paste the result, or expose an internal test-only construction path). **Cleaner alternative: change the generic parameter to a small placeholder type that `PasswordHasher<>` doesn't care about, since it's unused. Ask which the project prefers.**

- [ ] **Step 4: Run tests; commit**

```
git commit -m "feat(infra): PasswordHasherAdapter wraps PasswordHasher<T>"
```

---

## Task 8: Infrastructure — `JwtTokenService`

**Files:**
- Create: `backend/ePrevzem.Infrastructure/Identity/JwtTokenService.cs`
- Test: `backend/ePrevzem.Tests/Identity/Infrastructure/JwtTokenServiceTests.cs`

**Intent:** Issue access tokens from `JwtOptions` (already in `Api/Configuration`). Read what's there and either inject it into Infrastructure via DI or define a parallel options type in Infrastructure that binds the same section — **decide by looking at how Infrastructure currently consumes configuration; if unclear, ask.**

- [ ] **Step 1: Inspect `JwtOptions`**

Read `backend/ePrevzem.Api/Configuration/` and `Program.cs` for the existing JWT setup. Note issuer, audience, secret, lifetime conventions.

- [ ] **Step 2: Write failing tests**

- Access token: parses back to a JWT; contains `sub = SystemAdminId`, `role = "SystemAdmin"`, `iss`, `aud`, `exp`, `iat`; `ExpiresAt` matches `now + AccessTokenLifetimeMinutes`.
- Refresh token: plaintext is non-empty, hash is SHA-256(plaintext) base64-encoded, `ExpiresAt` matches `now + RefreshTokenLifetimeDays`.
- Two calls produce different refresh-token plaintexts (cryptographic randomness sanity check).

- [ ] **Step 3: Implement**

Use `JwtSecurityTokenHandler` (the same library already in use). For the refresh token: 32 bytes from `RandomNumberGenerator.GetBytes`, base64url-encode for plaintext, SHA-256 + base64 for hash. Pull lifetimes from a new options type bound to `Identity:AccessTokenLifetimeMinutes` and `Identity:RefreshTokenLifetimeDays`.

- [ ] **Step 4: Run tests; commit**

```
git commit -m "feat(infra): JwtTokenService issues access and refresh tokens"
```

---

## Task 9: Infrastructure — `IdentitySeeder`

**Files:**
- Create: `backend/ePrevzem.Infrastructure/Identity/IdentitySeeder.cs`

**Intent:** `IHostedService` that ensures a SystemAdmin exists on boot.

- [ ] **Step 1: Implement**

In `StartAsync`:
1. Create a scope; resolve `EPrevzemDbContext`, `IPasswordHasher`, `IClock`, `IdentityOptions`, `ILogger<IdentitySeeder>`.
2. If `await db.SystemAdmins.AnyAsync()` → log debug and return.
3. Else read `IdentityOptions.BootstrapAdmin.Username` and `:InitialPassword`. If either empty → throw `InvalidOperationException` with a message instructing the operator to set `Identity:BootstrapAdmin:InitialPassword` via user-secrets or env var.
4. Hash, `SystemAdmin.Create(...)`, persist, save. Log a warning that the bootstrap admin was created (do not log the password).

- [ ] **Step 2: Commit**

```
git commit -m "feat(infra): IdentitySeeder bootstraps the first admin from config"
```

(Tested via integration tests in Task 12.)

---

## Task 10: Infrastructure — DI wiring + options

**Files:**
- Create: `backend/ePrevzem.Api/Configuration/IdentityOptions.cs`
- Modify: `backend/ePrevzem.Infrastructure/DependencyInjection.cs`
- Modify: `backend/ePrevzem.Api/Program.cs`
- Modify: `backend/ePrevzem.Api/appsettings.json`
- Modify: `backend/ePrevzem.Api/appsettings.Development.json`

**Intent:** Register the adapters, hosted service, options, and current-user.

- [ ] **Step 1: Define `IdentityOptions`**

```csharp
public sealed class IdentityOptions
{
    public BootstrapAdminOptions BootstrapAdmin { get; init; } = new();
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 14;
}
public sealed class BootstrapAdminOptions
{
    public string Username { get; init; } = string.Empty;
    public string InitialPassword { get; init; } = string.Empty;
}
```

Place under `Api/Configuration/` if that matches existing options placement; otherwise wherever other options live. **If unclear, ask.**

- [ ] **Step 2: Bind options in `Program.cs`**

```csharp
builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
```

- [ ] **Step 3: Register adapters in `AddInfrastructure`**

In `Infrastructure/DependencyInjection.cs`:

```csharp
services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
services.AddSingleton<ITokenService, JwtTokenService>();
services.AddHostedService<IdentitySeeder>();
```

If `JwtTokenService` needs scoped `IOptions<IdentityOptions>` / `IOptions<JwtOptions>`, register as `Scoped` instead. **Match what the codebase does for similar adapters; ask if unclear.**

- [ ] **Step 4: Update `appsettings.json` (committed, empty secrets)**

```jsonc
"Identity": {
  "BootstrapAdmin": { "Username": "", "InitialPassword": "" },
  "AccessTokenLifetimeMinutes": 15,
  "RefreshTokenLifetimeDays": 14
}
```

- [ ] **Step 5: Update `appsettings.Development.json`**

Set `BootstrapAdmin.Username = "admin"` only. `InitialPassword` stays empty here — set via `dotnet user-secrets set "Identity:BootstrapAdmin:InitialPassword" "<dev-pass>"`.

- [ ] **Step 6: Build**

```
dotnet build ePrevzem.sln
```

- [ ] **Step 7: Commit**

```
git commit -m "feat(infra/api): wire Identity adapters, options, and HttpCurrentUser"
```

---

## Task 11: Api — `HttpCurrentUser` and `AdminAuthController`

**Files:**
- Create: `backend/ePrevzem.Api/Authentication/HttpCurrentUser.cs`
- Create: `backend/ePrevzem.Api/Controllers/Admin/AdminAuthController.cs`

**Intent:** Two anonymous endpoints. Thin controller. `HttpCurrentUser` reads JWT claims.

- [ ] **Step 1: Implement `HttpCurrentUser`**

Read `sub` claim as `Guid` for `UserId`. `OrganizationId` stays `null` for system admins (no claim). `IsAuthenticated` from `HttpContext.User.Identity?.IsAuthenticated`. `IsInRole(role)` from `User.IsInRole(role)`.

- [ ] **Step 2: Implement `AdminAuthController`**

Thin — parse request → `IMediator.Send` → map success to `Ok`, failure to `Problem` with the spec's `type` / `detail`. **Match the project's existing controller convention** (`ControllerBase` vs. minimal APIs, `IResult` vs. `IActionResult`, how errors are mapped). Ask if unclear.

Endpoints:
- `POST /api/admin/auth/login` — `[AllowAnonymous]`, body `LoginAdminRequest`.
- `POST /api/admin/auth/refresh` — `[AllowAnonymous]`, body `RefreshAdminTokenRequest`.

Request DTOs live where existing controllers keep them (controller-side records, or in `Application/Identity/Dtos`). Match existing convention.

- [ ] **Step 3: Build**

```
dotnet build ePrevzem.sln
```

- [ ] **Step 4: Commit**

```
git commit -m "feat(api): AdminAuthController exposes login and refresh"
```

---

## Task 12: Migration + integration tests

**Files:**
- Generated: `backend/ePrevzem.Infrastructure/Persistence/Migrations/<ts>_Identity_SystemAdminAndRefreshTokens.*`
- Create: `backend/ePrevzem.Tests/Identity/Integration/AdminAuthEndpointsTests.cs`
- Create: `backend/ePrevzem.Tests/Identity/Integration/IdentitySeederTests.cs`

**Intent:** Generate the migration and verify the full stack against a real Postgres via Testcontainers + `WebApplicationFactory`.

- [ ] **Step 1: Generate the migration**

```
dotnet ef migrations add Identity_SystemAdminAndRefreshTokens \
  --project backend/ePrevzem.Infrastructure \
  --startup-project backend/ePrevzem.Api
```

Inspect the generated up/down — sanity check tables, indexes, FKs against Task 6's configurations. Adjust the configurations and regenerate if needed (delete the migration files, fix, rerun).

- [ ] **Step 2: Write seeder integration tests**

Use the existing `WebApplicationFactory`-based test fixture. **Read one or two existing integration tests first** to match the fixture pattern.

Cases:
- Boot with no admin + valid bootstrap config → exactly one `SystemAdmin` exists with the configured username, `PasswordHash` set.
- Boot a second time with an admin already present → still exactly one admin; username/hash unchanged.
- Boot with no admin and empty `InitialPassword` → host startup throws (the test fixture should surface this; if not, adjust the assertion to inspect the seeder's `StartAsync` directly).

- [ ] **Step 3: Write endpoint integration tests**

Cases:
- `POST /login` with seeded admin's credentials → `200`, body has `AccessToken`, `RefreshToken`, expiry timestamps; DB has exactly one `RefreshToken` row whose `TokenHash` equals SHA-256 of the returned plaintext.
- `POST /login` wrong password → `401`, problem `type` matches `urn:eprevzem:identity:invalid-credentials`. No new refresh-token row.
- `POST /login` unknown user → same `401` shape (no enumeration).
- `POST /refresh` happy path → `200`, new pair; old `RefreshToken` row now has `RevokedAt` set and `ReplacedByTokenId` pointing at the new row.
- `POST /refresh` with the already-revoked previous token → `401`; entire chain ends up revoked.
- `POST /refresh` with an expired token → `401`. (Insert the row directly with `ExpiresAt` in the past to set this up.)
- Smoke: JWT issued by `/login` is accepted by a temporary `[Authorize(Roles = "SystemAdmin")]` endpoint added in the test project's startup customization (or against any existing admin-only endpoint once one lands).

- [ ] **Step 4: Run integration tests**

```
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Identity"
```

Fix anything red. Do not move on with failures.

- [ ] **Step 5: Commit**

```
git commit -m "feat(identity): integration tests + EF migration for admin auth"
```

---

## Task 13: Final pass

- [ ] **Step 1: Full build + full test run**

```
dotnet build ePrevzem.sln
dotnet test backend/ePrevzem.Tests
```

Everything green.

- [ ] **Step 2: Manual smoke (optional but recommended)**

```
dotnet user-secrets --project backend/ePrevzem.Api set "Identity:BootstrapAdmin:InitialPassword" "ChangeMe!1"
dotnet run --project backend/ePrevzem.Api
```

`curl -X POST http://localhost:5000/api/admin/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"ChangeMe!1"}'` → tokens. Use the refresh token against `/refresh`. Use the access token's `Authorization: Bearer <token>` against any admin-only endpoint once one exists.

- [ ] **Step 3: Verify dependency direction**

Quickly confirm:
- `Domain` references nothing outside itself (no EF, no Identity, no ASP.NET).
- `Application` references no EF, no ASP.NET, no `Microsoft.AspNetCore.Identity`.
- `Microsoft.AspNetCore.Identity` package is only in `ePrevzem.Infrastructure.csproj`.

If any of these are violated, fix before declaring done.

- [ ] **Step 4: Skim the spec for missed items**

`docs/superpowers/specs/2026-05-18-identity-system-admin-design.md` — section by section. If anything was missed, add a task and implement it. Common things to double-check:
- Failed-login audit event is actually raised when the username is unknown (not just when the password is wrong).
- Username normalization is consistent end-to-end (request → handler → DB lookup).
- `RefreshToken.TokenHash` is never logged or returned in any response.

- [ ] **Step 5: Done**

No final commit needed unless step 3 or 4 required fixes — those would have been their own commits.

---

## When to stop and ask

This plan deliberately leaves judgement calls in the implementer's hands for things that depend on conventions established elsewhere in the codebase. **Ask the user (don't decide silently) when you hit:**

- The codebase doesn't have a clear precedent for the choice (e.g. how to express handler failure, how to dispatch non-aggregate domain events, how to register options vs. plain configuration objects).
- The spec is ambiguous when read against the code (e.g. the exact ProblemDetails shape, where DTOs live).
- A library/package decision (e.g. exact NuGet package for `PasswordHasher<T>` in .NET 9, whether `JwtSecurityTokenHandler` or `JsonWebTokenHandler` is the project's standard).
- Anything that would expand scope beyond v1 (lockout, change-password, logout — all out of scope).

Better to pause for two minutes of clarification than to land a PR that needs to be reworked.
