namespace ePrevzem.Application.Organizations.Dtos;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string TaxNumber,
    string RegistrationNumber,
    TimeSpan DefaultPickupDuration,
    DateTimeOffset CreatedAt);
