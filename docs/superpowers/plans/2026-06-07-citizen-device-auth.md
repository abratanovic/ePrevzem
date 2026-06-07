# Citizen & Employee Device Auth — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the mobile app's fake registration/auth repositories with real backend integration: device onboarding from a code (citizen activation **or** employee provisioning) plus EC-P256 challenge/signature login issuing JWT access + refresh tokens.

**Architecture:** Backend adds a unified onboarding + device-auth API over the existing Clean Architecture (`Api → Application → Domain`, `Infrastructure → Application, Domain`), reusing the `RefreshToken` aggregate, `ITokenService`, and EF/Npgsql persistence. A new stateful `DeviceChallenge` aggregate makes challenge/verify replay-safe. Mobile adds a shared Ktor client, token/device storage, and HTTP repositories that satisfy the unchanged `RegistrationRepository` / `AuthRepository` interfaces.

**Tech Stack:** Backend — .NET 9, EF Core 9 (Npgsql), MediatR, FluentValidation, xUnit + Testcontainers. Mobile — Kotlin Multiplatform, Ktor client, kotlinx.serialization.

**Spec:** `docs/superpowers/specs/2026-06-07-citizen-device-auth-design.md`

---

## Parallelization Map (read first)

Two independent tracks run concurrently from the start, because the **wire contract below is frozen**:

- **Backend track** = tasks `B1…B17`.
- **Mobile track** = tasks `M1…M8`.

Within each track, tasks are grouped into **waves**. All tasks in a wave touch **disjoint files** and may be dispatched to parallel subagents simultaneously. A wave is a **barrier**: do not start wave N+1 until wave N is merged and green.

```
Backend:  [B1 B2]            wave B-I   (domain, parallel)
          [B3 B5 B6]         wave B-II  (ports + crypto + token + lookups, parallel)
           B4                 wave B-II' (SINGLE schema+migration task — see note)
          [B8 B9 B10 B11 B12] wave B-III (use cases, parallel)
          [B14 B15 B16] B13   wave B-IV  (controllers parallel; B13 DI separate)

Mobile:   [M1 M2]            wave M-I   (config + storage, parallel)
           M3                 wave M-II  (Ktor client; depends M1,M2)
          [M4]               wave M-II  (DTOs; independent, can join M-I)
          [M5 M6]            wave M-III (HTTP repos, parallel; depend M3,M4)
          [M7 M8]            wave M-IV  (wiring + tests)
```

**Critical serialization constraint — EF migrations.** Every `dotnet ef migrations add` rewrites the shared `EPrevzemDbContextModelSnapshot.cs`. Two migration tasks in parallel **will** conflict. Therefore **all schema changes are consolidated into the single task B4**, which depends on both B1 and B2. No other backend task may run `migrations add`.

**Shared-file constraint.** `DependencyInjection.cs` (B13), `EPrevzemDbContext.cs` (B4), `ITokenService.cs` (B5), `AppContainer.kt` (M7) are each touched by exactly one task to avoid merge races.

---

## Wire Contract (frozen — both tracks code against this)

Base path: backend root (e.g. `http://localhost:5xxx`). All bodies `application/json`. All endpoints `AllowAnonymous`.

### `GET /api/onboarding/{code}`
200:
```json
{ "kind": "Citizen", "firstName": "Marko", "lastName": "Horvat",
  "email": "marko.horvat@gmail.com", "phoneNumber": "+386 41 234 567",
  "organizationName": null, "roles": [], "expiresAt": "2026-06-08T10:00:00+00:00" }
```
For employees: `"kind": "Employee"`, `organizationName` set, `roles: ["Operator"]`, `phoneNumber: null`.
404 if code unknown; 410 if expired or already redeemed.

### `POST /api/onboarding/{code}/redeem`
Request:
```json
{ "publicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----",
  "deviceFingerprint": "android-abc123", "label": "Pixel 8" }
```
200 = **DeviceSessionResponse** (shared shape, also returned by verify & refresh):
```json
{ "role": "Citizen", "deviceId": "8f...e2",
  "accessToken": "eyJ...", "accessTokenExpiresAt": "2026-06-07T10:15:00+00:00",
  "refreshToken": "BASE64URL", "refreshTokenExpiresAt": "2026-06-21T10:00:00+00:00",
  "firstName": "Marko", "lastName": "Horvat",
  "organizationId": null, "organizationName": null,
  "roles": [] }
```
404 unknown code; 410 expired/redeemed; 400 validation (bad PEM/fingerprint).

### `POST /api/auth/device/challenge`
Request `{ "deviceId": "8f...e2" }` → 200 `{ "challenge": "BASE64", "expiresAt": "...". }`. 404 if device unknown/revoked.

### `POST /api/auth/device/verify`
Request `{ "deviceId": "8f...e2", "signature": "BASE64_DER_ECDSA" }` → 200 **DeviceSessionResponse**. 401 on bad/expired/consumed challenge or bad signature.

### `POST /api/auth/device/refresh`
Request `{ "refreshToken": "BASE64URL" }` → 200 **DeviceSessionResponse**. 401 on invalid/rotated/expired token.

**Crypto:** EC P-256 (`secp256r1`). `publicKeyPem` = X.509 SubjectPublicKeyInfo, PEM. `signature` = `SHA256withECDSA`, DER (`Rfc3279DerSequence`), base64. `challenge` = 32 random bytes, base64. Backend verifies `ECDsa.ImportSubjectPublicKeyInfo(der)` + `VerifyData(challenge, sig, SHA256, DSASignatureFormat.Rfc3279DerSequence)`.

---

# BACKEND TRACK

Run backend commands from repo root `C:\PROJEKTI\ePrevzem`.
Build check: `dotnet build ePrevzem.sln`. Tests: `dotnet test backend/ePrevzem.Tests`.

## Wave B-I — Domain

### Task B1: `DeviceChallenge` aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/DeviceChallengeId.cs`
- Create: `backend/ePrevzem.Domain/Identity/DeviceKind.cs`
- Create: `backend/ePrevzem.Domain/Identity/DeviceChallenge.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/DeviceChallengeTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// DeviceChallengeTests.cs
using ePrevzem.Domain.Identity;
using Xunit;

namespace ePrevzem.Tests.Domain.Identity;

public class DeviceChallengeTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_sets_fields_and_expiry()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Citizen,
            new byte[] { 1, 2, 3 }, Now, Now.AddMinutes(2));
        Assert.Null(c.ConsumedAt);
        Assert.True(c.IsConsumable(Now));
    }

    [Fact]
    public void Consume_marks_consumed_once()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Citizen,
            new byte[] { 1 }, Now, Now.AddMinutes(2));
        c.Consume(Now);
        Assert.False(c.IsConsumable(Now));
        Assert.Throws<InvalidOperationException>(() => c.Consume(Now));
    }

    [Fact]
    public void Expired_challenge_is_not_consumable()
    {
        var c = DeviceChallenge.Issue(DeviceChallengeId.New(), Guid.NewGuid(), DeviceKind.Employee,
            new byte[] { 1 }, Now, Now.AddMinutes(2));
        Assert.False(c.IsConsumable(Now.AddMinutes(3)));
    }
}
```

- [ ] **Step 2: Run, verify fail** — `dotnet test backend/ePrevzem.Tests --filter FullyQualifiedName~DeviceChallengeTests` → FAIL (type missing).

- [ ] **Step 3: Implement** (follow the strongly-typed-id pattern of `CitizenDeviceId.cs`).

```csharp
// DeviceChallengeId.cs
namespace ePrevzem.Domain.Identity;
public readonly record struct DeviceChallengeId(Guid Value)
{
    public static DeviceChallengeId New() => new(Guid.NewGuid());
}
```
```csharp
// DeviceKind.cs
namespace ePrevzem.Domain.Identity;
public enum DeviceKind { Citizen = 1, Employee = 2 }
```
```csharp
// DeviceChallenge.cs
using ePrevzem.Domain.Common;
namespace ePrevzem.Domain.Identity;

public sealed class DeviceChallenge : AggregateRoot<DeviceChallengeId>
{
    public Guid DeviceId { get; private set; }
    public DeviceKind DeviceKind { get; private set; }
    public byte[] Nonce { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private DeviceChallenge() { }

    public static DeviceChallenge Issue(DeviceChallengeId id, Guid deviceId, DeviceKind kind,
        byte[] nonce, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        if (nonce is null || nonce.Length == 0)
            throw new ArgumentException("Nonce is required.", nameof(nonce));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));
        return new DeviceChallenge
        {
            Id = id, DeviceId = deviceId, DeviceKind = kind,
            Nonce = nonce, CreatedAt = now, ExpiresAt = expiresAt
        };
    }

    public bool IsConsumable(DateTimeOffset at) => ConsumedAt is null && at < ExpiresAt;

    public void Consume(DateTimeOffset at)
    {
        if (ConsumedAt is not null)
            throw new InvalidOperationException("Challenge already consumed.");
        if (at >= ExpiresAt)
            throw new InvalidOperationException("Challenge has expired.");
        ConsumedAt = at;
    }
}
```

- [ ] **Step 4: Run, verify pass.**
- [ ] **Step 5: Commit** — `git commit -am "feat(identity): add DeviceChallenge aggregate"`

### Task B2: `RefreshToken` citizen support

**Files:**
- Modify: `backend/ePrevzem.Domain/Identity/RefreshToken.cs`
- Modify: `backend/ePrevzem.Domain/Identity/Events/RefreshTokenRotated.cs`
- Modify: `backend/ePrevzem.Domain/Identity/Events/RefreshTokenChainRevoked.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/RefreshTokenTests.cs` (add a case)

- [ ] **Step 1: Add failing test case** in `RefreshTokenTests.cs`:

```csharp
[Fact]
public void IssueForCitizen_sets_citizen_id_only()
{
    var now = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
    var citizenId = CitizenUserId.New();
    var t = RefreshToken.IssueForCitizen(RefreshTokenId.New(), citizenId, "hash", now.AddDays(14), now);
    Assert.Equal(citizenId, t.CitizenUserId);
    Assert.Null(t.EmployeeAccountId);
    Assert.Null(t.SystemAdminId);
    Assert.Null(t.OrganizationAdminAccountId);
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement.** In `RefreshToken.cs` add property `public CitizenUserId? CitizenUserId { get; private set; }` and factory:

```csharp
public static RefreshToken IssueForCitizen(
    RefreshTokenId id, CitizenUserId citizenUserId, string tokenHash,
    DateTimeOffset expiresAt, DateTimeOffset now)
{
    ValidateCommon(tokenHash, expiresAt, now);
    return new RefreshToken
    {
        Id = id, CitizenUserId = citizenUserId,
        TokenHash = tokenHash, ExpiresAt = expiresAt, CreatedAt = now
    };
}
```
Update `Rotate` and `RecordChainRevocation` to pass `CitizenUserId` into the two events. Add a nullable `CitizenUserId?` parameter (last position) to `RefreshTokenRotated` and `RefreshTokenChainRevoked` records and pass `CitizenUserId` at the call sites.

- [ ] **Step 4: Run full domain tests** — `dotnet test backend/ePrevzem.Tests --filter FullyQualifiedName~RefreshTokenTests` → PASS.
- [ ] **Step 5: Commit** — `git commit -am "feat(identity): RefreshToken.IssueForCitizen"`

## Wave B-II — Ports, crypto, token, lookups (parallel)

### Task B3: Signature verifier

**Files:**
- Create: `backend/ePrevzem.Application/Common/Abstractions/ISignatureVerifier.cs`
- Create: `backend/ePrevzem.Infrastructure/Identity/EcdsaSignatureVerifier.cs`
- Test: `backend/ePrevzem.Tests/Infrastructure/Identity/EcdsaSignatureVerifierTests.cs`

- [ ] **Step 1: Failing round-trip test** (generates a real P-256 key, signs, verifies):

```csharp
using System.Security.Cryptography;
using System.Text;
using ePrevzem.Infrastructure.Identity;
using Xunit;

namespace ePrevzem.Tests.Infrastructure.Identity;

public class EcdsaSignatureVerifierTests
{
    [Fact]
    public void Verifies_valid_p256_der_signature_over_challenge()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var pem = new string(PemEncoding.Write("PUBLIC KEY", spki));
        var challenge = Encoding.UTF8.GetBytes("hello-challenge");
        var sig = ecdsa.SignData(challenge, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        var verifier = new EcdsaSignatureVerifier();
        Assert.True(verifier.Verify(pem, challenge, sig));
        challenge[0] ^= 0xFF;
        Assert.False(verifier.Verify(pem, challenge, sig));
    }
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement.**
```csharp
// ISignatureVerifier.cs
namespace ePrevzem.Application.Common.Abstractions;
public interface ISignatureVerifier
{
    bool Verify(string publicKeyPem, byte[] data, byte[] signatureDer);
}
```
```csharp
// EcdsaSignatureVerifier.cs
using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
namespace ePrevzem.Infrastructure.Identity;

public sealed class EcdsaSignatureVerifier : ISignatureVerifier
{
    public bool Verify(string publicKeyPem, byte[] data, byte[] signatureDer)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);
            return ecdsa.VerifyData(data, signatureDer,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException) { return false; }
    }
}
```
Note: store `publicKeyPem` as UTF-8 bytes in `CitizenDevice.PublicKey` / `EmployeeDevice.PublicKey` so the verifier can re-read the PEM directly (decode with `Encoding.UTF8.GetString(device.PublicKey)`).

- [ ] **Step 4: Run, verify pass.**
- [ ] **Step 5: Commit** — `git commit -am "feat(identity): ECDSA P-256 signature verifier"`

### Task B5: Citizen access token

**Files:**
- Modify: `backend/ePrevzem.Application/Common/Abstractions/ITokenService.cs`
- Modify: `backend/ePrevzem.Infrastructure/Identity/JwtTokenService.cs`
- Test: `backend/ePrevzem.Tests/Infrastructure/Identity/JwtTokenServiceTests.cs` (add a case)

- [ ] **Step 1: Failing test** — issue a citizen token, assert `sub` = citizen id, role claim `Citizen`. Mirror existing employee test in that file.

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement.** Add to `ITokenService`: `AccessTokenResult IssueAccessToken(CitizenUser citizen);`. In `JwtTokenService` add (mirror the employee overload at `JwtTokenService.cs:81`):
```csharp
public AccessTokenResult IssueAccessToken(CitizenUser citizen)
{
    var now = _clock.UtcNow;
    var expiresAt = now.AddMinutes(_identityOptions.AccessTokenLifetimeMinutes);
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, citizen.Id.Value.ToString()),
        new Claim(ClaimTypes.Role, "Citizen"),
        new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };
    var token = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims,
        notBefore: now.UtcDateTime, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
    return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
}
```

- [ ] **Step 4: Run, verify pass.**
- [ ] **Step 5: Commit** — `git commit -am "feat(identity): issue citizen access token"`

### Task B6: Device-by-id lookups

**Files:**
- Modify: `backend/ePrevzem.Application/Common/Abstractions/ICitizenUserRepository.cs`
- Modify: `backend/ePrevzem.Application/Common/Abstractions/IEmployeeAccountRepository.cs`
- Modify: `backend/ePrevzem.Infrastructure/Identity/CitizenUserRepository.cs`
- Modify: `backend/ePrevzem.Infrastructure/Identity/EmployeeAccountRepository.cs`
- Also: `ICitizenActivationCodeRepository` / `IProvisioningCodeRepository` need `GetByCodeAsync` — verify they exist; if not, add (follow `CitizenUserRepository` pattern).

- [ ] **Step 1:** Add to `ICitizenUserRepository`:
```csharp
Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken cancellationToken = default);
Task<CitizenUser?> GetByIdWithDevicesAsync(CitizenUserId id, CancellationToken cancellationToken = default);
```
and to `IEmployeeAccountRepository`:
```csharp
Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default);
```
- [ ] **Step 2:** Implement with `.Include(...)` on the devices navigation. Example:
```csharp
public Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken ct = default)
    => _dbContext.CitizenUsers.Include("_devices")
        .FirstOrDefaultAsync(u => u.Devices.Any(d => d.Id == deviceId), ct);
```
(Confirm the backing field name for the devices collection from the existing EF config for `CitizenUser`; adjust the `Include` string accordingly. If a navigation is mapped, use the typed `.Include(u => u.Devices)`.)
- [ ] **Step 3:** Build: `dotnet build ePrevzem.sln`.
- [ ] **Step 4: Commit** — `git commit -am "feat(identity): device-by-id repository lookups"`

## Wave B-II' — Schema + migration (single task; depends B1, B2)

### Task B4: Persistence + one migration for DeviceChallenge and RefreshToken citizen column

**Files:**
- Create: `backend/ePrevzem.Infrastructure/Identity/Persistence/DeviceChallengeConfiguration.cs`
- Modify: `backend/ePrevzem.Infrastructure/Identity/Persistence/RefreshTokenConfiguration.cs`
- Modify: `backend/ePrevzem.Infrastructure/Persistence/EPrevzemDbContext.cs` (add `DbSet<DeviceChallenge> DeviceChallenges`)
- Create: `backend/ePrevzem.Infrastructure/Identity/DeviceChallengeRepository.cs`
- Create: `backend/ePrevzem.Application/Common/Abstractions/IDeviceChallengeRepository.cs`
- Generated: migration under `backend/ePrevzem.Infrastructure/Persistence/Migrations/`

- [ ] **Step 1:** `IDeviceChallengeRepository`:
```csharp
using ePrevzem.Domain.Identity;
namespace ePrevzem.Application.Common.Abstractions;
public interface IDeviceChallengeRepository
{
    Task AddAsync(DeviceChallenge challenge, CancellationToken cancellationToken = default);
    Task<DeviceChallenge?> GetLatestActiveAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
```
- [ ] **Step 2:** `DeviceChallengeRepository` (mirror `RefreshTokenRepository`). `GetLatestActiveAsync` returns the newest row for `deviceId` with `ConsumedAt == null && ExpiresAt > now` ordered by `CreatedAt` desc.
- [ ] **Step 3:** `DeviceChallengeConfiguration` — table `device_challenges`, `Id` value-converted like `RefreshTokenConfiguration.cs:17`, `Nonce` as `bytea`, `DeviceKind` stored as int, timestamps `timestamp with time zone`, index on `(DeviceId, ConsumedAt)`, `builder.Ignore(x => x.DomainEvents)`.
- [ ] **Step 4:** In `RefreshTokenConfiguration.cs` add the `citizen_user_id` column (mirror the `EmployeeAccountId` block at lines 67-71) + `HasOne<CitizenUser>().WithMany().HasForeignKey(x => x.CitizenUserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade)`, and extend the `CK_refresh_tokens_single_actor` check constraint to include the citizen column as a fourth exclusive arm.
- [ ] **Step 5:** Add `DbSet<DeviceChallenge> DeviceChallenges` to `EPrevzemDbContext`.
- [ ] **Step 6: Generate migration:**
```
dotnet ef migrations add Identity_AddDeviceChallengeAndCitizenRefreshToken --project backend/ePrevzem.Infrastructure --startup-project backend/ePrevzem.Api
```
- [ ] **Step 7:** `dotnet build ePrevzem.sln` → green.
- [ ] **Step 8: Commit** — `git commit -am "feat(identity): persistence + migration for device challenge & citizen refresh token"`

## Wave B-III — Application use cases (parallel)

> Shared DTO **first** (tiny, blocks B8-B12): create `backend/ePrevzem.Application/Identity/Dtos/DeviceSessionResponse.cs`:
```csharp
namespace ePrevzem.Application.Identity.Dtos;
public sealed record DeviceSessionResponse(
    string Role, Guid DeviceId,
    string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt,
    string FirstName, string LastName,
    Guid? OrganizationId, string? OrganizationName,
    IReadOnlyList<string> Roles);
```
Commit this before dispatching B8-B12.

### Task B8: `PeekOnboardingCode` query
**Files:** Create folder `backend/ePrevzem.Application/Identity/PeekOnboarding/` with `PeekOnboardingCodeQuery.cs` (query + `OnboardingPreview` result record + handler). Test: `backend/ePrevzem.Tests/Application/Identity/PeekOnboardingHandlerTests.cs`.
- [ ] Query: `record PeekOnboardingCodeQuery(string Code) : IRequest<OnboardingPreview>`.
- [ ] Handler: look up `ICitizenActivationCodeRepository.GetByCodeAsync` then `IProvisioningCodeRepository.GetByCodeAsync`. For citizen: load `CitizenUser`, return `kind=Citizen` with name + phone, `roles=[]`. For employee: return `kind=Employee`, `organizationName` (via `IOrganizationRepository`), `roles` (string names). Throw `OnboardingCodeNotFoundException` / `OnboardingCodeExpiredException` (define in this file) for 404/410 mapping.
- [ ] TDD: test citizen hit, employee hit, not-found, expired. Commit.

### Task B9: `RedeemOnboardingCode` command
**Files:** Create `backend/ePrevzem.Application/Identity/RedeemOnboarding/RedeemOnboardingCodeCommand.cs` (command + handler) + `RedeemOnboardingCodeValidator.cs`. Test: `RedeemOnboardingHandlerTests.cs`.
- [ ] Command: `record RedeemOnboardingCodeCommand(string Code, string PublicKeyPem, string DeviceFingerprint, string? Label) : IRequest<DeviceSessionResponse>`.
- [ ] Handler dispatch:
  - **Citizen:** `GetByCodeAsync` activation code → `IsRedeemable` else throw → load `CitizenUser` (with devices) → `citizen.RegisterDevice(CitizenDeviceId.New(), Encoding.UTF8.GetBytes(pem), fingerprint, label, now)` → `activationCode.Redeem(now)` → issue access (`IssueAccessToken(citizen)`) + refresh (`RefreshToken.IssueForCitizen`) → `DeviceSessionResponse(role:"Citizen", deviceId: device.Id.Value, …, roles: [])`.
  - **Employee:** `GetByCodeAsync` provisioning code → resolve target `EmployeeAccount`: if `RedeemedIntoEmployeeAccountId`/`IsReprovisioningOfEmployeeAccountId` set, load it; else create via `EmployeeAccount.Create(...)` from `PreFilledInfo` + `Roles` (+ `AddAsync`) → `account.RegisterDevice(EmployeeDeviceId.New(), …)` → `provisioningCode.Redeem(now, account.Id)` → issue access (`IssueAccessToken(account)`) + refresh (`RefreshToken.IssueForEmployee`) → response with `OrganizationId`, `OrganizationName`, `roles`.
- [ ] Validator: non-empty `PublicKeyPem` containing `BEGIN PUBLIC KEY`, non-empty `DeviceFingerprint`.
- [ ] TDD: citizen redeem issues tokens + registers device + marks code redeemed; employee redeem (resolve existing account) registers device; double-redeem throws. Commit.

> **Open item to verify here:** confirm whether `AddEmployeeMember` binds the code to the account via `RedeemedIntoEmployeeAccountId` or `IsReprovisioningOfEmployeeAccountId`. Read `OrgMembersController` + its `AddEmployeeMember` handler before writing the resolve branch; pick the field that is actually populated.

### Task B10: `IssueDeviceChallenge` command
**Files:** `backend/ePrevzem.Application/Identity/DeviceChallengeIssue/IssueDeviceChallengeCommand.cs` (+ handler). Test added in B17 integration (unit test optional).
- [ ] Command: `record IssueDeviceChallengeCommand(Guid DeviceId) : IRequest<DeviceChallengeResponse>` where `DeviceChallengeResponse(string Challenge, DateTimeOffset ExpiresAt)`.
- [ ] Handler: resolve device kind — try `GetByCitizenDeviceIdAsync(new CitizenDeviceId(deviceId))`; else `GetByEmployeeDeviceIdAsync(new EmployeeDeviceId(deviceId))`; if neither or device revoked → throw `DeviceNotFoundException`. Generate `RandomNumberGenerator.GetBytes(32)`, `DeviceChallenge.Issue(...)` with `now.AddMinutes(2)`, `AddAsync`, `SaveChanges`. Return base64 nonce + expiry. Commit.

### Task B11: `VerifyDeviceSignature` command
**Files:** `backend/ePrevzem.Application/Identity/DeviceVerify/VerifyDeviceSignatureCommand.cs` (+ handler).
- [ ] Command: `record VerifyDeviceSignatureCommand(Guid DeviceId, string SignatureBase64) : IRequest<DeviceSessionResponse>`.
- [ ] Handler: `GetLatestActiveAsync(deviceId, now)` → null ⇒ `InvalidChallengeException`. Resolve device + owner (citizen or employee) + its `PublicKey`. `ISignatureVerifier.Verify(Encoding.UTF8.GetString(device.PublicKey), challenge.Nonce, Convert.FromBase64String(sig))` → false ⇒ `InvalidSignatureException`. `challenge.Consume(now)`. Issue access+refresh for the owner; build `DeviceSessionResponse`. Commit (with unit test for consume-then-reject-replay).

### Task B12: `RefreshDeviceToken` command
**Files:** `backend/ePrevzem.Application/Identity/DeviceRefresh/RefreshDeviceTokenCommand.cs` (+ handler).
- [ ] Mirror `RefreshOrganizationAdminTokenCommand.cs` exactly, but branch on which owner id the `RefreshToken` carries (`CitizenUserId` vs `EmployeeAccountId`), reissue via the matching `IssueAccessToken` overload and `RefreshToken.IssueForCitizen`/`IssueForEmployee`. Reuse `InvalidRefreshTokenException`. Return `DeviceSessionResponse`. TDD: rotation happy path + reuse-detection revokes chain. Commit.

## Wave B-IV — API + DI (controllers parallel; B13 separate)

### Task B13: Dependency injection
**Files:** Modify `backend/ePrevzem.Infrastructure/DependencyInjection.cs`.
- [ ] Add: `services.AddScoped<IDeviceChallengeRepository, DeviceChallengeRepository>();` and `services.AddSingleton<ISignatureVerifier, EcdsaSignatureVerifier>();`. Build. Commit.

### Task B14: `OnboardingController`
**Files:** Create `backend/ePrevzem.Api/Controllers/Onboarding/OnboardingController.cs`. Route `api/onboarding`. Mirror error-to-ProblemDetails style of `OrgProvisioningController.cs` (404/410). Two actions: `GET {code}` → `PeekOnboardingCodeQuery`; `POST {code}/redeem` → `RedeemOnboardingCodeCommand`. Request record `RedeemOnboardingRequest(string PublicKeyPem, string DeviceFingerprint, string? Label)`. `[AllowAnonymous]`. Commit.

### Task B15: `DeviceAuthController`
**Files:** Create `backend/ePrevzem.Api/Controllers/Auth/DeviceAuthController.cs`. Route `api/auth/device`. Three `[AllowAnonymous]` actions: `POST challenge`, `POST verify`, `POST refresh` → the three commands. Map `DeviceNotFoundException`→404, `InvalidChallengeException`/`InvalidSignatureException`/`InvalidRefreshTokenException`→401. Request records `DeviceChallengeRequest(Guid DeviceId)`, `DeviceVerifyRequest(Guid DeviceId, string Signature)`, `DeviceRefreshRequest(string RefreshToken)`. Commit.

### Task B16: Rewrite `OrgProvisioningController.Redeem`
**Files:** Modify `backend/ePrevzem.Api/Controllers/Org/OrgProvisioningController.cs:98-105`. Replace the 501 body with a delegation to `RedeemOnboardingCodeCommand` (same request body as B14). Keep the existing `Peek` as-is. Commit.

> **Integration tests removed per user request (2026-06-07).** There is intentionally no Testcontainers/`WebApplicationFactory` endpoint test suite for this work. Validation of runtime behaviour that unit tests cannot cover — EF query translation for the `OwnsMany` device lookups, the migration applying cleanly, the `refresh_tokens` check constraint, and the full redeem→challenge→verify→refresh flow — relies on the **live smoke test in task M8** against the local backend. Handlers retain their unit tests with in-memory repository fakes.

---

# MOBILE TRACK

Run from `ePrevzemMobile/`. Compile check: `gradlew.bat :composeApp:compileCommonMainKotlinMetadata`. Tests: `gradlew.bat :composeApp:testDebugUnitTest`.

## Wave M-I — Config + storage (parallel)

### Task M1: Backend base URL config
**Files:** Modify `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/PlatformConfig.kt`, `…/androidMain/…/PlatformConfig.android.kt`, `…/iosMain/…/PlatformConfig.ios.kt`, and `composeApp/build.gradle.kts` (BuildConfig field).
- [ ] Add `val eprevzemApiBaseUrl: String` to the expect object. Android actual: `BuildConfig.EPREVZEM_API_BASE_URL` (add a `buildConfigField`/manifest placeholder following the existing `DIRECT4ME_API_KEY` wiring in `build.gradle.kts`). iOS actual: return a compile-time constant string (mirror how `direct4MeApiKey` is provided on iOS). Default to the live local backend URL, e.g. `http://10.0.2.2:5xxx` for the Android emulator. Compile check. Commit.

### Task M2: Token & device identity store
**Files:** Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStore.kt`. Test: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStoreTest.kt`.
- [ ] Wrap `SecureStorage` (same dependency as `PersistedSessionStore`). Keys: `auth.device_id`, `auth.access_token`, `auth.access_expires`, `auth.refresh_token`. API:
```kotlin
class DeviceSessionStore(private val storage: SecureStorage = SecureStorage()) {
    suspend fun saveSession(deviceId: String, accessToken: String, accessExpiresAt: String, refreshToken: String)
    suspend fun deviceId(): String?
    suspend fun accessToken(): String?
    suspend fun refreshToken(): String?
    suspend fun clear()
}
```
- [ ] TDD with a fake/in-memory `SecureStorage` if one exists in tests; else test on the JVM target. Commit.

### Task M4: Wire DTOs (can run in M-I)
**Files:** Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/api/OnboardingDtos.kt`.
- [ ] `@Serializable` records matching the contract: `OnboardingPreviewDto`, `RedeemRequestDto`, `DeviceSessionDto`, `ChallengeRequestDto`, `ChallengeResponseDto`, `VerifyRequestDto`, `RefreshRequestDto`. Use `explicitNulls = false` friendly nullable fields. Commit.

## Wave M-II — Ktor client

### Task M3: Shared API client
**Files:** Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/api/ApiClient.kt`. Depends M1, M2.
- [ ] Build an `HttpClient` (mirror `Direct4MeLockerRepository.kt:25-44`): `ContentNegotiation` json (`ignoreUnknownKeys`, `explicitNulls=false`), `HttpTimeout`. Add a `defaultRequest` that sets the base URL. Add bearer injection reading `DeviceSessionStore.accessToken()`, and an `Auth`/response-validator path that, on 401, calls `POST /api/auth/device/refresh` with the stored refresh token once, persists the new session, and retries. Expose the configured `HttpClient` + `baseUrl`. Compile check. Commit.

## Wave M-III — HTTP repositories (parallel)

### Task M5: `HttpRegistrationRepository`
**Files:** Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/registration/HttpRegistrationRepository.kt`. Test: `…/commonTest/…/HttpRegistrationRepositoryTest.kt` (Ktor `MockEngine`). Depends M3, M4, M2.
- [ ] Implement `RegistrationRepository`:
  - `validateCode(code)`: `GET /api/onboarding/{code}`; 200 ⇒ `Result.success(code)`; 404/410 ⇒ `Result.failure(InvalidCodeException())`.
  - `fetchAccountPreview(code)`: `GET /api/onboarding/{code}` → map `OnboardingPreviewDto` → `AppUser` (see mapping below).
  - `confirmAccount(code, publicKey)`: `POST /api/onboarding/{code}/redeem` with `RedeemRequestDto(publicKey, deviceFingerprint(), null)`; on 200 persist session via `DeviceSessionStore.saveSession(dto.deviceId, dto.accessToken, dto.accessTokenExpiresAt, dto.refreshToken)` and return mapped `AppUser`. On non-200 ⇒ `Result.failure(InvalidCodeException())`.
- [ ] **AppUser mapping (explicit defaults — backend returns fewer fields):**
```kotlin
fun DeviceSessionDto.toAppUser(): AppUser = when (role) {
    "Employee" -> AppUser.Employee(
        id = deviceId,
        fullName = "$firstName $lastName",
        email = "",                       // not returned; populated later via profile endpoint (sub-project B/C)
        phone = "",
        status = "Aktiven",
        validUntil = "",
        organizationId = organizationId?.toString() ?: "",
        organizationName = organizationName ?: "",
        organizationType = "",
        organizationLocation = "",
        roles = roles.mapNotNull { runCatching { EmployeeRole.valueOf(it) }.getOrNull() },
    )
    else -> AppUser.RegularUser(
        id = deviceId, fullName = "$firstName $lastName", email = "", phone = "",
    )
}
```
For `fetchAccountPreview`, map `OnboardingPreviewDto` similarly (email/phoneNumber are present there — use them; default the org-detail fields the backend omits to empty strings). `deviceFingerprint()` = a stable platform id (reuse an existing platform identifier if present; otherwise a persisted random UUID stored in `DeviceSessionStore`).
- [ ] TDD with `MockEngine` returning canned JSON for each case. Commit.

### Task M6: `HttpAuthRepository`
**Files:** Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/HttpAuthRepository.kt`. Test with `MockEngine`. Depends M3, M4, M2.
- [ ] Implement `AuthRepository`:
  - `getChallenge(deviceId)`: `POST /api/auth/device/challenge` with the **stored** deviceId (ignore the passed-in placeholder, or pass it through) → decode base64 `challenge` → `Result.success(bytes)`.
  - `verifySignature(deviceId, signature)`: `POST /api/auth/device/verify` with base64 signature → on 200 persist session via `DeviceSessionStore` and `Result.success(dto.accessToken)`; non-200 ⇒ `Result.failure`.
- [ ] TDD with `MockEngine`. Commit.

## Wave M-IV — Wiring + tests

### Task M7: Wire AppContainer + LoginRoute deviceId
**Files:** Modify `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/AppContainer.kt`, `…/feature/login/LoginRoute.kt`.
- [ ] In `AppContainer`: construct `DeviceSessionStore`, `ApiClient`, and swap:
```kotlin
val registrationRepository: RegistrationRepository = HttpRegistrationRepository(apiClient, deviceSessionStore)
val authRepository: AuthRepository = HttpAuthRepository(apiClient, deviceSessionStore)
```
Keep the `Fake*` classes in the tree (used by tests). Keep `lockerRepository` unchanged.
- [ ] In `LoginRoute.kt`: replace `private const val DEVICE_ID = "device-01"` usage — read the persisted device id from `AppContainer.deviceSessionStore.deviceId()` inside the coroutine before calling `getChallenge`; if null, route to reset/onboarding. Compile check. Commit.

### Task M8: Mobile end-to-end repo tests + smoke
- [ ] Ensure `gradlew.bat :composeApp:testDebugUnitTest` passes (M5/M6 tests).
- [ ] **Live smoke (manual, against B-track backend):** run the backend locally; from the app, onboard with a freshly issued code → confirm → kill/reopen → device login. Verify tokens persist and `device/challenge`+`verify` succeed. Record result in the PR description.

---

## Self-Review notes (author)

- **Spec coverage:** onboarding peek (B8/B14), redeem citizen+employee (B9/B14/B16), challenge (B10/B15), verify (B11/B15), refresh (B12/B15); domain `DeviceChallenge` (B1) + `RefreshToken` citizen (B2); infra verifier (B3), token (B5), persistence+migration (B4), lookups (B6), DI (B13); mobile config (M1), storage (M2), client (M3), DTOs (M4), repos (M5/M6), wiring (M7), tests (M8). All spec sections mapped.
- **Open items carried from spec:** employee resolve-vs-create field (verify in B9); keep `OrgProvisioning` peek (B16 keeps it). New mobile open item: `AppUser.Employee`/`RegularUser` carry fields the backend doesn't return yet (email/phone/org detail/validUntil) — mapped to explicit defaults in M5, to be backfilled by a citizen/employee profile endpoint in a later sub-project.
- **Type consistency:** `DeviceSessionResponse` (backend) ↔ `DeviceSessionDto` (mobile) share the same field set; `IssueForCitizen`, `IssueAccessToken(CitizenUser)`, `ISignatureVerifier.Verify`, `IDeviceChallengeRepository.GetLatestActiveAsync` names used consistently across tasks.
</content>
