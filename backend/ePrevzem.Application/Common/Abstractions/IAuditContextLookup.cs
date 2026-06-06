using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Application.Common.Abstractions;

public interface IAuditContextLookup
{
    Task<PackageAuditContext?> GetPackageAsync(PackageId packageId, CancellationToken cancellationToken = default);
    Task<EmployeeAuditContext?> GetEmployeeAsync(EmployeeAccountId employeeAccountId, CancellationToken cancellationToken = default);
    Task<OrganizationAdminAuditContext?> GetOrganizationAdminAsync(OrganizationAdminAccountId accountId, CancellationToken cancellationToken = default);
    Task<ProvisioningCodeAuditContext?> GetProvisioningCodeAsync(ProvisioningCodeId provisioningCodeId, CancellationToken cancellationToken = default);
    Task<CitizenActivationCodeAuditContext?> GetCitizenActivationCodeAsync(CitizenActivationCodeId codeId, CancellationToken cancellationToken = default);
    Task<StationClaimAuditContext?> GetStationClaimAsync(StationClaimId stationClaimId, CancellationToken cancellationToken = default);
    Task<LockerAuditContext?> GetLockerAsync(PickupStationId pickupStationId, LockerId lockerId, CancellationToken cancellationToken = default);
    Task<OrganizationAuditContext?> GetOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);
}

public sealed record PackageAuditContext(
    PackageId PackageId,
    OrganizationId OrganizationId,
    CitizenUserId RecipientCitizenUserId,
    EmployeeAccountId? CreatedByEmployeeAccountId,
    OrganizationAdminAccountId? CreatedByOrganizationAdminAccountId,
    string Description,
    string? OrganizationName,
    string? LockerLabel,
    string? Location);

public sealed record EmployeeAuditContext(
    EmployeeAccountId EmployeeAccountId,
    OrganizationId OrganizationId,
    ProvisioningCodeId CreatedFromProvisioningCodeId);

public sealed record OrganizationAdminAuditContext(
    OrganizationAdminAccountId OrganizationAdminAccountId,
    OrganizationId OrganizationId);

public sealed record ProvisioningCodeAuditContext(
    ProvisioningCodeId ProvisioningCodeId,
    OrganizationId OrganizationId,
    OrganizationAdminAccountId CreatedByOrganizationAdminId);

public sealed record CitizenActivationCodeAuditContext(
    CitizenActivationCodeId CitizenActivationCodeId,
    CitizenUserId CitizenUserId);

public sealed record StationClaimAuditContext(
    StationClaimId StationClaimId,
    PickupStationId PickupStationId,
    OrganizationId OrganizationId,
    string? LockerLabel,
    string? Location);

public sealed record LockerAuditContext(
    PickupStationId PickupStationId,
    LockerId LockerId,
    int LockerNumber,
    OrganizationId? OrganizationId,
    string? LockerLabel,
    string? Location);

public sealed record OrganizationAuditContext(OrganizationId OrganizationId, string Name);
