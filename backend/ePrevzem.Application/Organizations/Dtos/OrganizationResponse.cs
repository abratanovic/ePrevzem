namespace ePrevzem.Application.Organizations.Dtos;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    int DefaultPickupDurationDays,
    DateTimeOffset CreatedAt);
