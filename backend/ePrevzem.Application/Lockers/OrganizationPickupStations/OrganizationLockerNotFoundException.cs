namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed class OrganizationLockerNotFoundException(Guid lockerId)
    : Exception($"Locker '{lockerId}' was not found in the organization's pickup station.");
