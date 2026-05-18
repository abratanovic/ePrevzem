namespace ePrevzem.Application.Identity.Dtos;

public sealed record CreateOrganizationAdminResponse(
    Guid Id,
    string Email,
    Guid OrganizationId,
    string TemporaryPassword);
