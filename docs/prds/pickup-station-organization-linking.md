# PRD: Pickup Station — Organization Linking

## Problem Statement

When an organization purchases a physical pickup station, there is no way to register that station in the system and associate it with the purchasing organization. The platform has no concept of a claimed station: which organization owns it, where it is installed, and how many lockers it has. Without this, no pickups can be assigned to a station because the station does not exist in the system.

## Solution

Introduce a two-step flow:

1. A **system admin** pre-registers a pickup station by entering its serial number and the list of locker numbers printed on the physical unit. This creates the station record and its lockers in the system.
2. An **org admin** claims that station by entering its serial number along with the installation location (coordinates + address). This creates a time-stamped station claim linking the station to the organization, and records where it is physically installed.

## User Stories

1. As a system admin, I want to register a new pickup station by serial number, so that it becomes discoverable in the system before it is shipped to a customer.
2. As a system admin, I want to specify the locker numbers for a station at registration time, so that the physical numbering printed on the unit is preserved in the system.
3. As a system admin, I want the system to reject duplicate serial numbers, so that the same physical unit cannot be registered twice.
4. As a system admin, I want to see a structured response after registering a station, so that I can confirm the serial number and lockers were recorded correctly.
5. As an org admin, I want to claim a pickup station by entering its serial number, so that I can associate it with my organization.
6. As an org admin, I want to provide GPS coordinates when claiming a station, so that the system knows the precise location of the unit.
7. As an org admin, I want to provide a street address (address, house number, zip code, city) when claiming a station, so that humans can identify the installation location without needing coordinates.
8. As an org admin, I want to receive a clear error if the serial number I enter does not exist in the system, so that I know I entered the wrong number.
9. As an org admin, I want to receive a clear error if the station is already claimed by another organization, so that I know the unit is not available.
10. As an org admin, I want the claim to be timestamped, so that there is an auditable record of when my organization took ownership.
11. As an org admin, I want a structured response after claiming a station, so that I can confirm the station id, location, and claim timestamp.
12. As the platform, I want to raise a domain event when a station is claimed, so that other modules (audit, notifications) can react without coupling to the Lockers module.
13. As the platform, I want to enforce that only one organization can hold an active claim on a station at any given time, so that station ownership is unambiguous.

## Implementation Decisions

### Domain changes

- `PickupStation` aggregate gains a `SerialNumber` property (string, unique). Its `Create` factory is updated to accept the serial number. The `Location` property is removed from this aggregate.
- `StationClaim` aggregate gains a `Location` value object property, capturing where the station is installed at claim time. The `Claim` factory is updated to accept a `Location`.
- `PickupStation.Create` continues to accept the station id and a timestamp. Locker creation is handled via the existing `AddLocker` method, called once per locker number provided by the admin.
- The `Location` value object already enforces latitude/longitude range and non-empty address fields — no changes needed.

### New port interfaces (Application layer)

- `IPickupStationRepository`: `AddAsync`, `GetByIdAsync`, `GetBySerialNumberAsync`, `ExistsBySerialNumberAsync`
- `IStationClaimRepository`: `AddAsync`, `GetActiveClaimForStationAsync`

### New use cases (Application layer)

**RegisterPickupStation** (system admin)
- Input: `SerialNumber` (string), `LockerNumbers` (list of int)
- Validates serial number is non-empty and unique
- Validates locker numbers list is non-empty and contains no duplicates
- Creates `PickupStation`, calls `AddLocker` for each number
- Persists via repository, saves via unit of work
- Returns: station id, serial number, locker count

**ClaimPickupStation** (org admin)
- Input: `SerialNumber` (string), `Latitude`, `Longitude`, `Address`, `HouseNumber`, `ZipCode`, `City`
- Resolves the station by serial number — throws/returns 404 if not found
- Checks for an existing active claim — throws/returns 409 if found
- Builds `Location`, creates `StationClaim` via `StationClaim.Claim`, which raises `StationClaimed`
- Persists via repository, saves via unit of work
- Tenant context (`ITenantContext`) supplies the `OrganizationId`
- Returns: claim id, station id, organization id, location, claimed-at timestamp

### API layer

- `POST /api/admin/stations` — system admin, authorized with admin JWT
- `POST /api/org/stations` — org admin, authorized with org JWT; organization id taken from tenant context, not request body
- Both controllers are thin: validate → dispatch MediatR command → return 201 Created with response body

### Infrastructure

- EF Core entity configurations for `PickupStation`, `Locker`, and `StationClaim`
- `PickupStation.SerialNumber` has a unique index
- `Location` fields on `StationClaim` stored as owned entity (same table, no separate join)
- `Locker` stored as a child table with FK to `PickupStation`
- `StationClaim` has a partial unique index on `(PickupStationId)` WHERE `ReleasedAt IS NULL` to enforce one active claim per station at the database level
- All three are registered as `DbSet`s in `EPrevzemDbContext`
- EF migrations added via the standard `dotnet ef migrations add` convention
- Repository implementations in `Infrastructure/Lockers/Persistence/`

### FluentValidation

- `RegisterPickupStationCommandValidator`: serial number non-empty, locker numbers non-empty, no duplicates
- `ClaimPickupStationCommandValidator`: serial number non-empty, latitude in [-90, 90], longitude in [-180, 180], address fields non-empty

## Testing Decisions

Tests follow the project's existing pattern: **unit tests for Domain and Application** (in-memory repositories, `TestClock`, `TestUnitOfWork`); **no mocking of the database**.

Good tests verify external behavior only — what is persisted, what is returned, what exceptions are raised — not internal implementation steps.

### Domain tests (`Domain/Lockers/`)

- `PickupStation.Create` with valid arguments produces correct state
- `PickupStation.AddLocker` with a duplicate locker number throws
- `StationClaim.Claim` raises `StationClaimed` domain event with correct fields
- `StationClaim.Release` on an already-released claim throws
- `Location.Create` with out-of-range latitude/longitude throws
- `Location.Create` with blank address fields throws

### Application handler tests (`Application/Lockers/`)

- `RegisterPickupStationHandler`: valid command persists station and correct locker count
- `RegisterPickupStationHandler`: duplicate serial number throws (repo pre-seeded)
- `RegisterPickupStationHandler`: valid command calls `SaveChangesAsync`
- `RegisterPickupStationHandler`: response contains correct serial number and locker ids
- `ClaimPickupStationHandler`: valid command persists claim with correct org id and location
- `ClaimPickupStationHandler`: unknown serial number throws not-found exception
- `ClaimPickupStationHandler`: already-claimed station throws conflict exception
- `ClaimPickupStationHandler`: valid command calls `SaveChangesAsync`
- `ClaimPickupStationHandler`: response contains correct claim timestamp from clock

Prior art: `CreateOrganizationHandlerTests` (same pattern — in-memory repo, `TestClock`, `TestUnitOfWork`, assertions on repo state and return value).

## Out of Scope

- Releasing / unclaiming a station (org admin or system admin)
- Updating location after a station is claimed
- Listing stations for an organization
- Listing all stations (system admin view)
- Adding or removing lockers after registration
- Marking lockers out of service
- Integration with the Direct4.me locker gateway
- Notifications triggered by `StationClaimed` domain event
- Audit log entries for station registration or claiming

## Further Notes

- The `StationClaim` aggregate's `Release` method and `StationReleased` domain event are already modeled in the domain and should not be removed — they will be used in a future release.
- The partial unique index on `StationClaim` (active-claim enforcement) acts as a database-level safety net in addition to the application-level check in the handler.
- The `ProvisioningCode` aggregate stores `StationAccess` as a JSON list of `PickupStationId`s — this referencing pattern is compatible with the new schema and requires no migration changes.
