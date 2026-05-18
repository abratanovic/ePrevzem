# PRD: Organization Admin & Member Registration

## Problem Statement

There is currently no way for organizations to have their own users in the system. SystemAdmins can create organizations, but there is no identity layer below SystemAdmin — no one can log into the platform on behalf of an organization, manage that organization's members, or provision mobile devices for field workers.

## Solution

Introduce two new actor types below SystemAdmin:

1. **OrganizationAdmin** — a web portal user created by SystemAdmin, who manages their organization's members and station registrations.
2. **OrganizationMember** — a mobile-device worker (Operator or RecordManager) provisioned by an OrganizationAdmin via a short-lived provisioning code.

SystemAdmin creates an OrganizationAdmin by entering their details; the system generates a temporary password that SystemAdmin sends out-of-band. The OrganizationAdmin logs into the web portal, is prompted to change their password, and can then issue provisioning codes for their members. A member enters the code on their mobile device, the app peeks the pre-filled info, and a second call (implemented later) completes device provisioning.

## User Stories

1. As a SystemAdmin, I want to create an OrganizationAdmin account by entering their name, email, and organization, so that the organization has a designated portal user.
2. As a SystemAdmin, I want the system to generate a temporary password for a new OrganizationAdmin, so that I can send it to them out-of-band without choosing one myself.
3. As a SystemAdmin, I want the temporary password to be returned once in the API response, so that I can copy it and hand it to the admin.
4. As an OrganizationAdmin, I want to log in to the web portal with my email and password, so that I can manage my organization.
5. As an OrganizationAdmin, I want to be prompted to change my password on first login, so that the system-generated temporary password does not remain in use.
6. As an OrganizationAdmin, I want my JWT to carry my organization ID, so that all my requests are automatically scoped to my organization.
7. As an OrganizationAdmin, I want to refresh my access token using a refresh token, so that my session stays alive without re-entering my password.
8. As an OrganizationAdmin, I want to change my password at any time, so that I can maintain account security.
9. As an OrganizationAdmin, I want to create a provisioning code for a new member by entering their first name, last name, optional email, role(s), and station access, so that the member can self-provision their device.
10. As an OrganizationAdmin, I want the provisioning code to have an expiry time, so that unused codes cannot be redeemed indefinitely.
11. As an OrganizationAdmin, I want the provisioning code to be a human-readable short code, so that a member can type it on their device without error.
12. As an OrganizationAdmin, I want the provisioning code to carry pre-filled member data (name, email, roles, station access), so that the member does not need to re-enter information already known to the org.
13. As an OrganizationAdmin, I want to be able to issue a re-provisioning code for an existing member, so that they can re-link their device if it is lost or replaced.
14. As an OrganizationMember, I want to enter a provisioning code on my device and immediately see my pre-filled name, roles, and organization, so that I can confirm the details are correct before completing setup.
15. As an OrganizationMember, I want the peek endpoint to be publicly accessible (no prior login required), so that a brand-new device with no credentials can initiate the flow.
16. As an OrganizationMember, I want an expired or already-redeemed code to be rejected with a clear error, so that I know I need to request a new one.

## Implementation Decisions

### New Aggregate: OrganizationAdminAccount
- Fields: first name, last name, email (used as login username), password hash, organization ID, must-change-password flag, status (Active/Disabled), created-at timestamp.
- Methods: Create (sets must-change-password = true), SetPassword (clears must-change-password), RecordLogin, Disable, Reenable.
- Lives in the Identity bounded context.
- Email must be unique across all OrganizationAdminAccounts.

### Modified Aggregate: ProvisioningCode
- `CreatedByEmployeeAccountId` renamed to `CreatedByOrganizationAdminId` — only OrganizationAdmins issue provisioning codes.
- No other behavioral changes.

### Modified Aggregate: RefreshToken
- Add an optional `OrganizationAdminAccountId` field alongside the existing optional `SystemAdminId`.
- Invariant: exactly one of the two actor fields is set.
- Same rotation/expiry logic applies to both actor types.

### Modified Enum: EmployeeAccountRole
- Remove `OrganizationAdmin` value.
- Remaining values: `RecordManager`, `Operator`.
- `EmployeeAccount` is now exclusively for mobile-device members.

### Modified Interface: ITokenService
- Add overload to issue access tokens for `OrganizationAdminAccount`.
- OrganizationAdmin access tokens carry claims: `sub` (account ID), `role` = `OrganizationAdmin`, `organizationId`.
- `HttpCurrentUser` reads the `organizationId` claim to populate `ITenantContext` for tenant-scoped EF global filters.

### API Contracts

**POST /api/admin/org-admins** (SystemAdmin JWT required)
- Request: firstName, lastName, email, organizationId
- Response: account ID, email, organization ID, generated temporary password (returned once)

**POST /api/org/auth/login** (public)
- Request: email, password
- Response: access token, refresh token, mustChangePassword flag

**POST /api/org/auth/refresh** (public)
- Request: refresh token
- Response: new access token, new refresh token

**POST /api/org/auth/change-password** (OrganizationAdmin JWT required)
- Request: current password, new password
- Response: 204 No Content

**POST /api/org/provisioning-codes** (OrganizationAdmin JWT required)
- Request: firstName, lastName, email (optional), roles, stationAccess (list of station IDs), expiresIn (duration)
- Response: provisioning code string, expires-at timestamp

**GET /api/org/provisioning/{code}** (public)
- Response: pre-filled firstName, lastName, email, roles, organizationId, organizationName, expiresAt

**POST /api/org/provisioning/{code}/redeem** (public) — **OUT OF SCOPE, stub only**

### Schema Changes
- New table: `OrganizationAdminAccounts`
- `ProvisioningCodes` table: rename FK column `CreatedByEmployeeAccountId` → `CreatedByOrganizationAdminId`, update FK target
- `RefreshTokens` table: add nullable FK `OrganizationAdminAccountId`; add check constraint ensuring exactly one actor FK is non-null

## Testing Decisions

**What makes a good test:** test observable behavior and outcomes, not internal implementation. A domain unit test should invoke public aggregate methods and assert on raised domain events and property state. An application-layer test should mock ports and assert on commands/queries. Integration tests should hit a real Postgres instance via Testcontainers and assert on HTTP responses and DB state.

**Domain unit tests (no DB):**
- `OrganizationAdminAccount`: Create sets MustChangePassword; SetPassword clears it; Disable/Reenable guard against invalid transitions; duplicate-login recording emits correct events.
- `ProvisioningCode`: Issue with `OrganizationAdminId` emits event; Redeem happy path and all guard cases (expired, already redeemed).
- `RefreshToken`: both actor variants (SystemAdmin, OrganizationAdmin) rotate and revoke correctly.
- Prior art: `ePrevzem.Tests/Domain/Identity/SystemAdminTests.cs`, `ProvisioningCodeTests.cs`

**Application handler unit tests:**
- CreateOrganizationAdmin: generates password, hashes it, returns plaintext once.
- LoginOrganizationAdmin: wrong password, disabled account, mustChangePassword surfaced in response.
- RefreshOrganizationAdminToken: expired token, chain-revoked token rejected.
- ChangeOrganizationAdminPassword: wrong current password rejected, MustChangePassword cleared.
- IssueProvisioningCode: non-admin caller rejected (authorization), code written to repo.
- PeekProvisioningCode: expired code returns 404/410, redeemed code returns 410.
- Prior art: `ePrevzem.Tests/Application/Identity/LoginAdminHandlerTests.cs`, `RefreshAdminTokenHandlerTests.cs`

**Integration tests (Testcontainers Postgres + WebApplicationFactory):**
- Full create-OrgAdmin → login → change-password flow.
- Full issue-provisioning-code → peek flow.
- Prior art: `ePrevzem.Tests/` integration tests pattern.

## Out of Scope

- **Provisioning code redeem** (`POST /api/org/provisioning/{code}/redeem`) — stub endpoint with TODO comment only; device registration and member account creation are deferred.
- **Email delivery** — temporary password and provisioning codes are returned in API responses only; no SMTP/email service integration.
- **OrganizationAdmin management by SystemAdmin** (list, disable, delete org admins) — creation only in this iteration.
- **OrganizationMember management UI** — listing, disabling, or editing existing members is deferred.
- **Password reset / forgot-password flow** — deferred.
- **PickupStation registration** — covered by a separate upcoming feature.

## Further Notes

- The `ProvisioningCode` aggregate already exists in the domain with a well-tested lifecycle. The only domain change is the creator reference — all other logic (expiry, single-use, re-provisioning) is unchanged.
- The `MustChangePassword` flag is surfaced in the login response so the web portal can redirect immediately without a separate profile-check call.
- Tenant isolation for OrganizationAdmin requests is handled automatically by the existing EF global query filter infrastructure once `organizationId` is embedded in the JWT and `ITenantContext` reads it from `HttpCurrentUser`.
