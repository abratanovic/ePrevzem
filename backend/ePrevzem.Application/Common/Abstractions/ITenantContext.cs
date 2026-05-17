namespace ePrevzem.Application.Common.Abstractions;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
}
