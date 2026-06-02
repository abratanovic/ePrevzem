namespace ePrevzem.Application.Pickups.Dtos;

public sealed record PickupRecipientResponse(Guid Id, string FirstName, string LastName);

public sealed record PickupStationOptionResponse(Guid Id, string Name, string Address);

public sealed record PickupResponse(
    Guid Id,
    string Reference,
    string Description,
    string RecipientName,
    string LocationName,
    string Status,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset CreatedAt,
    bool CanDelete,
    bool CanCancel);

public sealed record DashboardStatsResponse(
    int ActivePickups,
    int ActivePickupsTrend,
    int PendingPickups,
    int PendingExpiresToday,
    int OccupiedLockers,
    int TotalLockers,
    int ExpiredThisWeek);

public sealed record LockerOccupancyResponse(
    Guid Id,
    string StationId,
    string Name,
    int Used,
    int Total);
