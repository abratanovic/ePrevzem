# 09 — Infrastructure: persist PickupStation, Locker, StationClaim

## What to build

Wire all three Lockers aggregates into EF Core: entity configurations, DbSets, and a migration. Enforce uniqueness on serial number and the one-active-claim-per-station invariant at the database level.

## Acceptance criteria

- [ ] `PickupStation` has an EF configuration: serial number column with unique index
- [ ] `Locker` has an EF configuration: FK to `PickupStation`, `LockerNumber` column
- [ ] `StationClaim` has an EF configuration: `Location` stored as owned entity (same table), `ReleasedAt` nullable, partial unique index on `PickupStationId` WHERE `ReleasedAt IS NULL`
- [ ] `EPrevzemDbContext` registers `DbSet<PickupStation>`, `DbSet<Locker>`, `DbSet<StationClaim>`
- [ ] Migration is added and the schema builds cleanly against a fresh database

## Blocked by

- Blocked by #08
