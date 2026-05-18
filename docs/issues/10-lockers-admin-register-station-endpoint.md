# 10 — feat(lockers): add POST /api/admin/stations

## What to build

End-to-end slice for the system admin registering a pickup station. Covers the port interface, use-case command + handler + validator, infrastructure repository, thin controller, and unit tests.

## Acceptance criteria

- [ ] `IPickupStationRepository` port defined in Application with `AddAsync`, `GetByIdAsync`, `GetBySerialNumberAsync`, `ExistsBySerialNumberAsync`
- [ ] `RegisterPickupStationCommand` accepts `SerialNumber` (string) and `LockerNumbers` (list of int)
- [ ] `RegisterPickupStationCommandValidator` rejects: empty serial number, empty locker list, duplicate locker numbers
- [ ] Handler creates `PickupStation`, calls `AddLocker` for each number, persists, saves
- [ ] Handler returns a response DTO with station id, serial number, and list of locker ids
- [ ] Handler throws a domain exception on duplicate serial number
- [ ] EF Core repository implements `IPickupStationRepository`
- [ ] `POST /api/admin/stations` controller dispatches to MediatR, returns 201 Created
- [ ] Controller is protected by system admin JWT authorization
- [ ] Unit tests: valid command persists correct station + lockers, duplicate serial number throws and does not persist, `SaveChangesAsync` is called, response fields are correct

## Blocked by

- Blocked by #08
- Blocked by #09
