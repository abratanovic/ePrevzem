# Identity Feature — System Admin (v1)

**Date:** 2026-05-18
**Scope:** `backend/` only. v1 covers system-admin authentication. Citizen-user and employee-account authentication are deliberately out of scope and accommodated via additive design.

## Goals

- Authenticate the system-admin role with username + password.
- Issue short-lived JWT access tokens and rotating refresh tokens.
- Bootstrap the first admin from configuration on startup.
- Follow `backend/AGENTS.md` Clean Architecture rules. Keep Domain free of EF/auth concerns.
- Leave room for adding `EmployeeAccount` (password-based) and `CitizenUser` (SI-TRUST-driven) authentication later without rework of v1 surfaces.

## Non-goals (v1)

- ASP.NET Identity. Rejected as overkill for a single role with no email reset, lockout, 2FA, or external logins. The same PBKDF2 hashing is reused via `Microsoft.AspNetCore.Identity.PasswordHasher<T>` as a standalone hasher.
- Change-password endpoint. The seeded bootstrap password is permanent until a future endpoint is added or it is rewritten in DB. **Recommended first follow-up.**
- Logout endpoint. Refresh tokens expire on their own; can be added later as a one-token revoke.
- Lockout / brute-force protection. Failed-login auditing covers the observability gap; rate-limiting can be added later without schema change.
- Forgot-password / email flows. No email infrastructure in scope.
- MFA / 2FA.
- Admin-management endpoints (create / list / deactivate admins).

## Architectural shape

Identity feature module spans the four backend projects per `AGENTS.md`.

```
Api ─▶ Application ─▶ Domain
 │           ▲
 └─▶ Infrastructure ─▶ Application, Domain
```

- **Domain/Identity** — `SystemAdmin` aggregate (extended), `RefreshToken` aggregate (new), domain events. Zero external dependencies. EF/Identity/JWT concerns absent.
- **Application/Identity** — MediatR use cases (`LoginAdminCommand`, `RefreshAdminTokenCommand`), DTOs, FluentValidation validators. Ports: `IPasswordHasher`, `ITokenService` (new in `Application/Common/Abstractions/`).
- **Infrastructure/Identity** — `PasswordHasherAdapter` (wraps `PasswordHasher<SystemAdmin>`), `JwtTokenService`, `IdentitySeeder` (`IHostedService`), EF configurations, hash-based refresh-token storage.
- **Api** — `AdminAuthController` (thin), `HttpCurrentUser` adapter for `ICurrentUser`, `IdentityOptions` binding.

### Future-role accommodation

- `IPasswordHasher` and `ITokenService` are role-agnostic and reusable by a future `LoginEmployeeCommand`.
- `CitizenUser` SI-TRUST flow will issue tokens via `ITokenService` without `IPasswordHasher`.
- Each role gets its own login endpoint and command — no forced sharing of a user table. Adding citizen/employee auth is additive: new aggregates already exist, new ports already shaped.
- Refresh-token table can be made polymorphic later (discriminator on subject kind) or each role can own a sibling refresh-token aggregate. Deferred until that work lands.

## Domain model

```
SystemAdmin (existing aggregate, extended)
  Id              SystemAdminId
  Username        string                 // unique, normalized lowercase
  PasswordHash    string                 // opaque, produced by IPasswordHasher
  CreatedAt       DateTimeOffset
  LastLoginAt     DateTimeOffset?
  + Create(id, username, passwordHash, now)
  + SetPassword(string hash, DateTimeOffset now)        // raises SystemAdminPasswordChanged
  + RecordLogin(DateTimeOffset now)                     // raises SystemAdminLoggedIn

RefreshToken (new aggregate, Domain/Identity)
  Id                  RefreshTokenId           (Guid)
  SystemAdminId       SystemAdminId
  TokenHash           string                   // SHA-256(plaintext) base64
  ExpiresAt           DateTimeOffset
  CreatedAt           DateTimeOffset
  RevokedAt           DateTimeOffset?
  ReplacedByTokenId   RefreshTokenId?
  + Issue(id, systemAdminId, tokenHash, expiresAt, now)
  + Rotate(RefreshTokenId replacementId, DateTimeOffset now)   // raises RefreshTokenRotated
  + Revoke(DateTimeOffset now)
  IsActive(now) => RevokedAt is null && ExpiresAt > now
```

`RefreshToken` is a sibling aggregate (not a child collection of `SystemAdmin`). Cross-aggregate reference by id only.

**Domain events**

- `SystemAdminPasswordChanged(SystemAdminId, At)`
- `SystemAdminLoggedIn(SystemAdminId, At)`
- `SystemAdminLoginFailed(AttemptedUsername, At)` — raised by the handler when no match or wrong password; consumed by audit pipeline.
- `RefreshTokenRotated(OldTokenId, NewTokenId, SystemAdminId, At)`
- `RefreshTokenChainRevoked(SystemAdminId, TriggerTokenId, At)`

## Persistence

ASP.NET Identity is NOT used. `EPrevzemDbContext` is a plain `DbContext` (no `IdentityDbContext`).

- `SystemAdminConfiguration` — table `system_admins`. `Username` unique index, stored lowercase-on-write (no `citext` extension dependency). `PasswordHash` `text NOT NULL`.
- `RefreshTokenConfiguration` — table `refresh_tokens`. Unique index on `TokenHash`. Index on `(SystemAdminId, RevokedAt)`. FK `SystemAdminId → system_admins(Id) ON DELETE CASCADE`.

Both `DbSet<SystemAdmin>` and `DbSet<RefreshToken>` added to `EPrevzemDbContext` and to `IEPrevzemDbContext`.

Migration:

```
dotnet ef migrations add Identity_SystemAdminAndRefreshTokens \
  --project backend/ePrevzem.Infrastructure \
  --startup-project backend/ePrevzem.Api
```

No data seeded by the migration. Seeding happens at runtime via `IdentitySeeder` to keep credentials out of source.

## Application layer

### Ports (`Application/Common/Abstractions/`)

```csharp
public interface IPasswordHasher
{
    string Hash(string plaintext);
    PasswordVerification Verify(string hash, string plaintext);
}
public enum PasswordVerification { Failed, Success, NeedsRehash }

public interface ITokenService
{
    AccessTokenResult IssueAccessToken(SystemAdmin admin);
    RefreshTokenResult IssueRefreshToken(DateTimeOffset now);
}
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenResult(string Plaintext, string Hash, DateTimeOffset ExpiresAt);
```

`IEPrevzemDbContext` gains `DbSet<SystemAdmin> SystemAdmins` and `DbSet<RefreshToken> RefreshTokens`.

### Use cases (`Application/Identity/`)

**`LoginAdminCommand(Username, Password) → AdminTokenResponse`**

1. Normalize username (`Trim().ToLowerInvariant()`).
2. Load `SystemAdmin` by username.
3. If not found: run `IPasswordHasher.Verify` against a constant dummy hash (timing equalization), raise `SystemAdminLoginFailed`, return `INVALID_CREDENTIALS`.
4. `Verify(admin.PasswordHash, password)`:
   - `Failed` → raise `SystemAdminLoginFailed`, return `INVALID_CREDENTIALS`.
   - `NeedsRehash` → rehash, `admin.SetPassword(newHash, now)`.
   - `Success` → continue.
5. `admin.RecordLogin(now)`.
6. Issue access + refresh; persist new `RefreshToken` (hash only).
7. `SaveChangesAsync`. Return tokens.

**`RefreshAdminTokenCommand(RefreshToken) → AdminTokenResponse`**

1. SHA-256 the incoming plaintext.
2. Look up `RefreshToken` by hash.
3. If not found → `401 INVALID_REFRESH_TOKEN`.
4. If `RevokedAt is not null`: walk `ReplacedByTokenId` chain forward, revoke each active descendant. Raise `RefreshTokenChainRevoked`. Return `401`.
5. If `ExpiresAt <= now` → `401`.
6. Load owning `SystemAdmin`. If missing → `401`.
7. Issue new access + refresh pair. `oldToken.Rotate(newToken.Id, now)`. Persist new token.
8. `SaveChangesAsync`. Return tokens.

### Validators

- `LoginAdminRequest`: `Username` not empty, ≤ 128 chars; `Password` not empty, ≤ 256 chars.
- `RefreshAdminTokenRequest`: `RefreshToken` not empty.

### Error semantics

Unknown username and wrong password collapse to one response:

```
401 ProblemDetails
  type:   urn:eprevzem:identity:invalid-credentials
  title:  Invalid credentials
  detail: "Napačno uporabniško ime ali geslo."
```

`400` for validation failures. `401 INVALID_REFRESH_TOKEN` for any refresh failure. No user enumeration.

## Infrastructure layer

- **`PasswordHasherAdapter : IPasswordHasher`** — wraps `Microsoft.AspNetCore.Identity.PasswordHasher<SystemAdmin>`. `Hash` calls `HashPassword`. `Verify` maps `VerifyHashedPassword` results: `Success → Success`, `SuccessRehashNeeded → NeedsRehash`, `Failed → Failed`.
- **`JwtTokenService : ITokenService`** — issues JWTs from existing `JwtOptions` (issuer/audience/secret). Access-token lifetime from `Identity:AccessTokenLifetimeMinutes`. Claims: `sub = SystemAdminId`, `role = SystemAdmin`, standard `iss`/`aud`/`exp`/`iat`. Refresh token = 32 random bytes (`RandomNumberGenerator`) → base64url plaintext + SHA-256 base64 hash + `now + RefreshTokenLifetimeDays`.
- **`IdentitySeeder : IHostedService`** — on boot:
  - If any `SystemAdmin` exists, return.
  - Else read `Identity:BootstrapAdmin:Username` + `:InitialPassword`. If password is empty, throw — fail loud.
  - Hash, persist, log a single warning that bootstrap admin was created.
- EF configurations as above.

## API layer

```
POST /api/admin/auth/login      [AllowAnonymous]
POST /api/admin/auth/refresh    [AllowAnonymous]
```

`AdminAuthController` is thin: parse → `IMediator.Send` → `Results.Ok` / `Results.Problem`.

**DTOs**

```csharp
public sealed record LoginAdminRequest(string Username, string Password);
public sealed record RefreshAdminTokenRequest(string RefreshToken);
public sealed record AdminTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
```

**`HttpCurrentUser : ICurrentUser`** — reads JWT claims via `IHttpContextAccessor`. `UserId` from `sub`. `OrganizationId` null for system admins. `IsInRole("SystemAdmin")` → true when role claim matches. Registered in `Program.cs`.

**`Program.cs` changes**
- Bind `Identity` configuration section to `IdentityOptions`.
- Register `IHttpContextAccessor`, `HttpCurrentUser`.
- Authorization service already in place; future role policies layered on `[Authorize(Roles = "SystemAdmin")]`.

## Configuration

`appsettings.json` (committed, empty secrets):

```jsonc
"Identity": {
  "BootstrapAdmin": { "Username": "", "InitialPassword": "" },
  "AccessTokenLifetimeMinutes": 15,
  "RefreshTokenLifetimeDays": 14
}
```

`appsettings.Development.json` may set `Username = "admin"`; `InitialPassword` always via user-secrets locally and env var in deployed environments. `IdentitySeeder` throws on startup when there is no admin and no `InitialPassword`.

## Cross-cutting

- **Audit log.** Domain events (`SystemAdminLoggedIn`, `SystemAdminLoginFailed`, `SystemAdminPasswordChanged`, `RefreshTokenRotated`, `RefreshTokenChainRevoked`) are consumed by the existing append-only audit pipeline. Failed logins audited including attempted username — observability for brute-force without v1 lockout machinery.
- **Multi-tenancy.** System admins are cross-tenant; no `OrganizationId` on `SystemAdmin`. Tenant filter unaffected.
- **Secrets.** Empty in committed `appsettings.json`; populated via user-secrets / env vars per `AGENTS.md`.

## Testing

**Unit (no DB)**
- `SystemAdmin.SetPassword` raises `SystemAdminPasswordChanged`; `RecordLogin` sets `LastLoginAt`.
- `RefreshToken.Rotate` sets `RevokedAt` + `ReplacedByTokenId`; `IsActive` honors expiry and revocation.
- Validators: empty / oversized inputs.

**Integration (`ePrevzem.Tests`, Testcontainers Postgres + `WebApplicationFactory`)**
- `IdentitySeeder` creates bootstrap admin on first boot; idempotent on second boot; throws when no admin exists and `InitialPassword` is empty.
- `POST /login` happy path → returns valid JWT, refresh token stored as hash (not plaintext).
- `POST /login` wrong password → `401`; same response shape for unknown user (no enumeration).
- `POST /login` unknown user → `401`; smoke check that hashing path still ran (timing equalization not strictly asserted).
- `POST /refresh` happy path → new pair issued, old token revoked with `ReplacedByTokenId`.
- `POST /refresh` with already-revoked token → chain revoked, `401`.
- `POST /refresh` with expired token → `401`.
- `PasswordHasher` `NeedsRehash` path → hash transparently upgraded after a successful login.
- JWT issued by `/login` is accepted by `[Authorize(Roles = "SystemAdmin")]` (temporary test endpoint).

No DB mocking, per `AGENTS.md`.

## Files added / modified

```
Domain/Identity/
  SystemAdmin.cs                                          (modify)
  RefreshToken.cs                                         (new)
  RefreshTokenId.cs                                       (new)
  Events/SystemAdminPasswordChanged.cs                    (new)
  Events/SystemAdminLoggedIn.cs                           (new)
  Events/SystemAdminLoginFailed.cs                        (new)
  Events/RefreshTokenRotated.cs                           (new)
  Events/RefreshTokenChainRevoked.cs                      (new)

Application/Common/Abstractions/
  IPasswordHasher.cs                                      (new)
  ITokenService.cs                                        (new)
  IEPrevzemDbContext.cs                                   (modify: add DbSets)

Application/Identity/
  Login/LoginAdminCommand.cs + Handler + Validator        (new)
  Refresh/RefreshAdminTokenCommand.cs + Handler + Validator (new)
  Dtos/AdminTokenResponse.cs                              (new)
  Dtos/LoginAdminRequest.cs                               (new)
  Dtos/RefreshAdminTokenRequest.cs                        (new)

Infrastructure/Identity/
  PasswordHasherAdapter.cs                                (new)
  JwtTokenService.cs                                      (new)
  IdentitySeeder.cs                                       (new, IHostedService)
  Persistence/SystemAdminConfiguration.cs                 (new)
  Persistence/RefreshTokenConfiguration.cs                (new)
Infrastructure/Persistence/EPrevzemDbContext.cs           (modify: DbSets)
Infrastructure/DependencyInjection.cs                     (modify: register adapters + hosted service)
Infrastructure/Persistence/Migrations/<ts>_Identity_SystemAdminAndRefreshTokens.* (new)

Api/
  Controllers/Admin/AdminAuthController.cs                (new)
  Configuration/IdentityOptions.cs                        (new)
  Authentication/HttpCurrentUser.cs                       (new)
  Program.cs                                              (modify: bind IdentityOptions, register HttpCurrentUser)
appsettings.json                                          (modify: Identity section, empty secrets)
appsettings.Development.json                              (modify: dev Username only)
```

## Known follow-ups (out of scope, recommended order)

1. `POST /api/admin/auth/change-password` — remove the permanence of the bootstrap password.
2. `POST /api/admin/auth/logout` — revoke a single refresh token.
3. Rate-limiting / lockout on `/login` once metrics suggest a need.
4. Employee-account login command using the same `IPasswordHasher` + `ITokenService`.
5. Citizen-user login via SI-TRUST issuing tokens through `ITokenService` (no password).
