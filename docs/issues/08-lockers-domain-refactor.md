# 08 — Domain: refactor PickupStation + StationClaim

## What to build

Update the two existing domain aggregates to match the final model: `PickupStation` gets a `SerialNumber` property and loses `Location`; `StationClaim` gains `Location`. Update both factory methods (`Create` / `Claim`) accordingly.

## Acceptance criteria

- [ ] `PickupStation` has a `SerialNumber` (string) property
- [ ] `PickupStation.Create` accepts `serialNumber` and no longer accepts `location`
- [ ] `PickupStation` has no `Location` property
- [ ] `StationClaim` has a `Location` value object property
- [ ] `StationClaim.Claim` factory accepts a `Location` argument and stores it
- [ ] `StationClaimed` domain event carries the `Location`
- [ ] All existing domain compilation errors from the refactor are resolved

## Blocked by

None — can start immediately.
