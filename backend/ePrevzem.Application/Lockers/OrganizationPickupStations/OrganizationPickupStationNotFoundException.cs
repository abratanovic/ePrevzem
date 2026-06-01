namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed class OrganizationPickupStationNotFoundException(Guid claimId)
    : Exception($"Active pickup station claim '{claimId}' was not found for this organization.");
