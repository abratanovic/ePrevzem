namespace ePrevzem.Application.Pickups.Dtos;

/// <summary>
/// Everything an Operator needs after scanning a pickup-station QR: the station,
/// the packages awaiting placement there, and the lockers free to receive one.
/// </summary>
public sealed record InsertionContextResponse(
    Guid StationId,
    string SerialNumber,
    string LocationName,
    IReadOnlyList<InsertionPackageResponse> Packages,
    IReadOnlyList<FreeLockerResponse> FreeLockers);

public sealed record InsertionPackageResponse(
    Guid Id,
    string Reference,
    string Description,
    string RecipientName);

public sealed record FreeLockerResponse(Guid LockerId, int LockerNumber);

/// <summary>
/// The audio token (base64-encoded WAV) the client plays to actuate the lock.
/// Returned by both the citizen open and the employee insertion-open endpoints.
/// </summary>
public sealed record LockerTokenResponse(string TokenBase64);

/// <summary>Result of persisting an employee insertion (package → InLocker).</summary>
public sealed record InsertionConfirmedResponse(
    Guid PackageId,
    string Reference,
    string Status,
    DateTimeOffset? DeadlineAt);
