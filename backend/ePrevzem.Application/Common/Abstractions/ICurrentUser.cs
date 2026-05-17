namespace ePrevzem.Application.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
