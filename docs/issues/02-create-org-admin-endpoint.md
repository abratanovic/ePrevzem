# Issue 02: POST /api/admin/org-admins — create OrganizationAdmin

**Type:** AFK
**Blocked by:** Issue 01
**Commit message:** `feat(identity): add POST /api/admin/org-admins endpoint`

## What to build

Allow a SystemAdmin to create an OrganizationAdmin account. The system generates a random temporary password, hashes it, and returns the plaintext once in the response. All layers: EF config + migration, repository, application command + handler + validator, controller, and tests.

## Acceptance criteria

- [ ] `OrganizationAdminAccount` is persisted via a new EF configuration and migration (`Identity_AddOrganizationAdminAccount`). Table: `OrganizationAdminAccounts`.
- [ ] `IOrganizationAdminAccountRepository` port defined in `Application/Common/Abstractions/` with at least `AddAsync` and `GetByEmailAsync`.
- [ ] `CreateOrganizationAdminCommand` accepted by `POST /api/admin/org-admins` (SystemAdmin JWT required). Fields: `firstName`, `lastName`, `email`, `organizationId`.
- [ ] Handler generates a cryptographically random temporary password (min 12 chars, mixed case + digits + symbol).
- [ ] Handler validates that the email is not already taken; returns a domain-problem response if duplicate.
- [ ] Response includes: `id`, `email`, `organizationId`, `temporaryPassword` (plaintext, returned once).
- [ ] `FluentValidation` validator rejects blank names, invalid email format, empty `organizationId`.
- [ ] Application handler unit test: happy path returns temp password; duplicate email returns conflict; missing org returns validation error.
- [ ] Integration test: `POST /api/admin/org-admins` with valid payload → 201, row exists in DB, `temporaryPassword` present in response.

## Blocked by

- Blocked by Issue 01
