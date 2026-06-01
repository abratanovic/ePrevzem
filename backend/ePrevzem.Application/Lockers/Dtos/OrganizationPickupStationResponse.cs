using ePrevzem.Domain.Lockers;

namespace ePrevzem.Application.Lockers.Dtos;

public sealed record OrganizationPickupStationResponse(
    Guid ClaimId,
    Guid StationId,
    Guid OrganizationId,
    string SerialNumber,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrganizationStationLockerResponse> Lockers,
    LocationResponse Location,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? ReleasedAt)
{
    public static OrganizationPickupStationResponse From(StationClaim claim, PickupStation station)
        => new(
            claim.Id.Value,
            station.Id.Value,
            claim.OrganizationId.Value,
            station.SerialNumber,
            station.CreatedAt,
            station.Lockers
                .OrderBy(locker => locker.LockerNumber)
                .Select(locker => new OrganizationStationLockerResponse(
                    locker.Id.Value,
                    locker.LockerNumber,
                    locker.IsServiceable))
                .ToList(),
            new LocationResponse(
                claim.Location.Latitude,
                claim.Location.Longitude,
                claim.Location.Address,
                claim.Location.HouseNumber,
                claim.Location.ZipCode,
                claim.Location.City),
            claim.ClaimedAt,
            claim.ReleasedAt);
}

public sealed record OrganizationStationLockerResponse(
    Guid Id,
    int LockerNumber,
    bool IsServiceable);
