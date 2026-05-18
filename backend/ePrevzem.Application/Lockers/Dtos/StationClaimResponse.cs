namespace ePrevzem.Application.Lockers.Dtos;

public sealed record StationClaimResponse(
    Guid ClaimId,
    Guid StationId,
    Guid OrganizationId,
    LocationResponse Location,
    DateTimeOffset ClaimedAt);

public sealed record LocationResponse(
    decimal Latitude,
    decimal Longitude,
    string Address,
    string HouseNumber,
    string ZipCode,
    string City);
