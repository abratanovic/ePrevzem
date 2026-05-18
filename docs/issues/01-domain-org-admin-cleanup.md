# Issue 01: Domain — OrganizationAdminAccount + cleanup

**Type:** AFK
**Blocked by:** None — can start immediately
**Commit message:** `feat(identity): add OrganizationAdminAccount aggregate, clean up EmployeeAccountRole and ProvisioningCode`

## What to build

Introduce the `OrganizationAdminAccount` aggregate into the Identity bounded context. Also apply two breaking domain cleanups that are prerequisites for all later slices: remove the `OrganizationAdmin` value from `EmployeeAccountRole` (since that role is now a separate aggregate, not an `EmployeeAccount` role), and rename `ProvisioningCode.CreatedByEmployeeAccountId` to `CreatedByOrganizationAdminId`.

This slice is purely domain — no infrastructure, no migrations, no HTTP endpoints.

## Acceptance criteria

- [ ] `OrganizationAdminAccount` aggregate exists in `Domain/Identity/` with fields: first name, last name, email, password hash, organization ID, must-change-password flag, status (Active/Disabled), created-at.
- [ ] `OrganizationAdminAccount.Create(...)` sets `MustChangePassword = true` and raises an `OrganizationAdminAccountCreated` domain event.
- [ ] `OrganizationAdminAccount.SetPassword(...)` updates the hash and sets `MustChangePassword = false`, raises `OrganizationAdminPasswordChanged`.
- [ ] `OrganizationAdminAccount.RecordLogin(...)` updates `LastLoginAt` and raises `OrganizationAdminLoggedIn`.
- [ ] `OrganizationAdminAccount.Disable(...)` and `Reenable(...)` guard against invalid transitions and raise corresponding events.
- [ ] `EmployeeAccountRole` enum no longer contains `OrganizationAdmin`; only `RecordManager` and `Operator` remain.
- [ ] `ProvisioningCode` aggregate field renamed from `CreatedByEmployeeAccountId` to `CreatedByOrganizationAdminId` (type changes from `EmployeeAccountId` to `OrganizationAdminAccountId`).
- [ ] `ProvisioningCode.Issue(...)` factory signature updated accordingly.
- [ ] All existing domain tests for `ProvisioningCode` and `EmployeeAccount` still pass (updated to reflect renamed field/removed role).
- [ ] New domain unit tests cover: Create sets MustChangePassword; SetPassword clears it; Disable/Reenable guard cases; all events raised correctly.

## Blocked by

None — can start immediately.
