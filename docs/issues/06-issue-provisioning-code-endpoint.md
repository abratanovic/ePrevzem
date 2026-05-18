# Issue 06: POST /api/org/provisioning-codes — issue provisioning code

**Type:** AFK
**Blocked by:** Issues 02, 03
**Commit message:** `feat(identity): add POST /api/org/provisioning-codes endpoint`

## What to build

Allow an OrganizationAdmin to issue a provisioning code for a new OrganizationMember. The code carries pre-filled member data (name, email, roles, station access) and has a configurable expiry. This slice also adds the migration that renames the `ProvisioningCodes.CreatedByEmployeeAccountId` FK column to `CreatedByOrganizationAdminId`.

## Acceptance criteria

- [ ] Migration `Identity_ProvisioningCodeUseOrgAdminCreator` renames the FK column and updates the FK reference to `OrganizationAdminAccounts`.
- [ ] `IProvisioningCodeRepository` port defined (or extended) with `AddAsync` and `GetByCodeAsync`.
- [ ] `IssueProvisioningCodeCommand` handled by `POST /api/org/provisioning-codes` (OrganizationAdmin JWT required). Fields: `firstName`, `lastName`, `email` (optional), `roles` (list of `RecordManager` | `Operator`), `stationAccess` (list of station IDs), `expiresInHours`.
- [ ] Handler generates a short human-readable code (e.g. 8 uppercase alphanumeric chars, no ambiguous chars like 0/O/I/1).
- [ ] Handler passes `OrganizationAdminAccountId` (from JWT) as the creator reference to `ProvisioningCode.Issue(...)`.
- [ ] Response: `code`, `expiresAt`.
- [ ] `FluentValidation` validator: names required, at least one role, `expiresInHours` between 1 and 168 (1 week).
- [ ] Application handler unit tests: happy path; empty roles rejected; invalid station ID handled.
- [ ] Integration test: `POST /api/org/provisioning-codes` → 201, code in response, row in DB with correct `CreatedByOrganizationAdminId`.

## Blocked by

- Blocked by Issue 02
- Blocked by Issue 03
