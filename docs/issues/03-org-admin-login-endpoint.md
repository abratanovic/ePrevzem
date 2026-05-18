# Issue 03: POST /api/org/auth/login — OrganizationAdmin login

**Type:** AFK
**Blocked by:** Issue 01
**Commit message:** `feat(identity): add POST /api/org/auth/login endpoint`

## What to build

Allow an OrganizationAdmin to log in with email + password. Issues a JWT access token carrying `organizationId` and role claims, plus a refresh token. Extends `ITokenService` with an `OrganizationAdminAccount` overload. Extends the `RefreshToken` aggregate to support OrganizationAdmin as an actor. Wires `organizationId` from JWT into `ITenantContext` via `HttpCurrentUser`.

## Acceptance criteria

- [ ] `ITokenService` has a new overload `IssueAccessToken(OrganizationAdminAccount admin)` that embeds claims: `sub` (account ID), `role` = `OrganizationAdmin`, `organizationId`.
- [ ] `RefreshToken` aggregate has an optional `OrganizationAdminAccountId` field alongside existing `SystemAdminId`; exactly one must be set (enforced in factory method and EF check constraint).
- [ ] Migration `Identity_AddOrgAdminRefreshTokenSupport` adds the nullable FK column and check constraint to `RefreshTokens`.
- [ ] `HttpCurrentUser` extracts `organizationId` claim and exposes it; `ITenantContext` is populated for all OrganizationAdmin requests.
- [ ] `LoginOrganizationAdminCommand` handled by `POST /api/org/auth/login` (public, no JWT). Fields: `email`, `password`.
- [ ] Response: `accessToken`, `expiresAt`, `refreshToken`, `mustChangePassword`.
- [ ] Wrong password → 401. Disabled account → 401. Unknown email → 401 (same error, no enumeration).
- [ ] `FluentValidation` validator rejects blank email/password.
- [ ] Application handler unit tests: happy path; wrong password; disabled account; `mustChangePassword` true when flag is set.
- [ ] `RefreshToken` domain unit tests: OrganizationAdmin variant rotates and chain-revokes correctly.
- [ ] Integration test: login → 200 with tokens; decode JWT, verify `organizationId` and `role` claims present.

## Blocked by

- Blocked by Issue 01
