# Issue 07: GET /api/org/provisioning/{code} — peek provisioning code + stub redeem

**Type:** AFK
**Blocked by:** Issue 06
**Commit message:** `feat(identity): add GET /api/org/provisioning/{code} endpoint`

## What to build

Allow a mobile device to peek a provisioning code before committing to it. The endpoint is public (no JWT required — the code itself is the credential). Returns pre-filled member info so the app can display a confirmation screen. Also add a stub `POST /api/org/provisioning/{code}/redeem` endpoint that returns 501 Not Implemented with a TODO comment for the device-provisioning flow.

## Acceptance criteria

- [ ] `PeekProvisioningCodeQuery` handled by `GET /api/org/provisioning/{code}` (public, no auth).
- [ ] Response: `firstName`, `lastName`, `email`, `roles`, `organizationId`, `organizationName`, `expiresAt`.
- [ ] Code not found → 404.
- [ ] Code expired → 410 Gone with Slovenian error message.
- [ ] Code already redeemed → 410 Gone with Slovenian error message.
- [ ] `POST /api/org/provisioning/{code}/redeem` stub exists, returns 501 Not Implemented. Controller method has a `// TODO: implement device provisioning` comment.
- [ ] Application handler unit tests: happy path returns pre-filled data; expired code → domain error mapped to 410; redeemed code → 410.
- [ ] Integration test: issue code → peek → 200 with correct pre-filled fields; peek expired code → 410; peek unknown code → 404.

## Blocked by

- Blocked by Issue 06
