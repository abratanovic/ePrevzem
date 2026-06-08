namespace ePrevzem.Application.Pickups.Dtos;

/// <summary>
/// A pickup as seen by the citizen recipient on their home screen. Unlike
/// <see cref="PickupResponse"/> (organization-facing), this carries the
/// organization name, the physical locker number, and a precomputed
/// expiring-soon flag the mobile client renders directly.
/// </summary>
public sealed record CitizenPickupResponse(
    Guid Id,
    string Reference,
    string Description,
    string OrganizationName,
    string LocationName,
    string LocationAddress,
    int? LockerNumber,
    string Status,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset CreatedAt,
    bool IsExpiringSoon);

/// <summary>
/// Full detail of a single pickup for the citizen detail screen. Adds the
/// pickup timestamp (when the citizen collected it) on top of the list shape.
/// </summary>
public sealed record CitizenPickupDetailResponse(
    Guid Id,
    string Reference,
    string Description,
    string OrganizationName,
    string LocationName,
    string LocationAddress,
    decimal Latitude,
    decimal Longitude,
    int? LockerNumber,
    string Status,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PickedUpAt,
    bool IsExpiringSoon);
