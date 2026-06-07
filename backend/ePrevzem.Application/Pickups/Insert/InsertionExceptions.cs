namespace ePrevzem.Application.Pickups.Insert;

/// <summary>The current user may not perform the insertion (not an active Operator of the station's org).</summary>
public sealed class InsertionForbiddenException()
    : Exception("The current user cannot insert packages at this station.");

/// <summary>The station serial is unknown or the org has no active claim on it.</summary>
public sealed class InsertionStationNotFoundException()
    : Exception("Pickup station not found or not claimed by the organization.");

/// <summary>The chosen locker is not free (occupied / out of service / wrong station).</summary>
public sealed class LockerUnavailableException()
    : Exception("The chosen locker is not available.");
