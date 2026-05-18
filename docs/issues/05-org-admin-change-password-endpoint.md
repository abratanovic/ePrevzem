# Issue 05: POST /api/org/auth/change-password — change OrganizationAdmin password

**Type:** AFK
**Blocked by:** Issue 03
**Commit message:** `feat(identity): add POST /api/org/auth/change-password endpoint`

## What to build

Allow an OrganizationAdmin to change their password. Verifies the current password, sets the new one, and clears the `MustChangePassword` flag. Required for completing the first-login forced-change flow.

## Acceptance criteria

- [ ] `ChangeOrganizationAdminPasswordCommand` handled by `POST /api/org/auth/change-password` (OrganizationAdmin JWT required). Fields: `currentPassword`, `newPassword`.
- [ ] Handler verifies `currentPassword` against stored hash before applying change.
- [ ] On success: `OrganizationAdminAccount.SetPassword(...)` called, `MustChangePassword` cleared, 204 returned.
- [ ] Wrong current password → 400 with clear error.
- [ ] `FluentValidation` validator enforces minimum password strength on `newPassword` (min 8 chars).
- [ ] Application handler unit tests: happy path clears flag; wrong current password rejected.
- [ ] Integration test: login (mustChangePassword = true) → change password → login again → mustChangePassword = false in response.

## Blocked by

- Blocked by Issue 03
