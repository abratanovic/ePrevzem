# Issue 04: POST /api/org/auth/refresh — refresh OrganizationAdmin token

**Type:** AFK
**Blocked by:** Issue 03
**Commit message:** `feat(identity): add POST /api/org/auth/refresh endpoint`

## What to build

Allow an OrganizationAdmin to exchange a valid refresh token for a new access + refresh token pair. Mirrors the existing `POST /api/admin/auth/refresh` flow for SystemAdmin.

## Acceptance criteria

- [ ] `RefreshOrganizationAdminTokenCommand` handled by `POST /api/org/auth/refresh` (public). Field: `refreshToken`.
- [ ] Handler locates the refresh token by hash, validates it belongs to an OrganizationAdmin actor, checks expiry and chain-revocation status.
- [ ] On success: old token is rotated (marked used), new access + refresh token pair returned.
- [ ] Expired token → 401. Already-used token → 401 (chain revoked). Token belonging to a SystemAdmin actor → 401.
- [ ] Application handler unit tests: happy path; expired; chain-revoked; wrong actor type.
- [ ] Integration test: login → refresh → new tokens valid; second refresh with old token → 401.

## Blocked by

- Blocked by Issue 03
