namespace ePrevzem.Application.Lockers.Dtos;

public sealed record PickupStationResponse(
    Guid Id,
    string SerialNumber,
    IReadOnlyList<LockerResponse> Lockers,
    DateTimeOffset CreatedAt);

public sealed record LockerResponse(Guid Id, int LockerNumber);
