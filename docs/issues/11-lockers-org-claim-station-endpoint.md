# 11 — feat(lockers): add POST /api/org/stations

## What to build

End-to-end slice for the org admin claiming a pickup station. Covers the port interface, use-case command + handler + validator, infrastructure repository, thin controller, and unit tests.

## Acceptance criteria

- [ ] `IStationClaimRepository` port defined in Application with `AddAsync`, `GetActiveClaimForStationAsync`
- [ ] `ClaimPickupStationCommand` accepts `SerialNumber`, `Latitude`, `Longitude`, `Address`, `HouseNumber`, `ZipCode`, `City`
- [ ] `ClaimPickupStationCommandValidator` rejects: empty serial number, latitude outside [-90, 90], longitude outside [-180, 180], empty address fields
- [ ] Handler resolves station by serial number — throws not-found exception if missing
- [ ] Handler checks for an existing active claim — throws conflict exception if found
- [ ] Handler reads `OrganizationId` from `ITenantContext`, not from the request body
- [ ] Handler creates `Location`, creates `StationClaim` via `StationClaim.Claim`, persists, saves
- [ ] `StationClaimed` domain event is raised
- [ ] Handler returns a response DTO with claim id, station id, organization id, location, and claimed-at timestamp
- [ ] EF Core repository implements `IStationClaimRepository`
- [ ] `POST /api/org/stations` controller dispatches to MediatR, returns 201 Created
- [ ] Controller is protected by org admin JWT authorization
- [ ] Unit tests: valid command persists claim with correct org id and location, unknown serial number throws not-found, already-claimed station throws conflict, `SaveChangesAsync` is called, response timestamp matches clock

## Blocked by

- Blocked by #08
- Blocked by #09
- Blocked by #10
