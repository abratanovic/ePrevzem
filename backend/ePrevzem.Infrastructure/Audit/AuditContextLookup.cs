using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Audit;

public sealed class AuditContextLookup : IAuditContextLookup
{
    private readonly EPrevzemDbContext _dbContext;

    public AuditContextLookup(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PackageAuditContext?> GetPackageAsync(
        PackageId packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _dbContext.Packages.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == packageId, cancellationToken);
        if (package is null) return null;

        var organizationName = await _dbContext.Organizations.AsNoTracking()
            .Where(x => x.Id == package.OrganizationId)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var claim = await _dbContext.StationClaims.AsNoTracking()
            .Where(x => x.PickupStationId == package.TargetPickupStationId
                && x.OrganizationId == package.OrganizationId
                && x.ClaimedAt <= package.CreatedAt
                && (x.ReleasedAt == null || x.ReleasedAt >= package.CreatedAt))
            .OrderByDescending(x => x.ClaimedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var stationSerial = await _dbContext.PickupStations.AsNoTracking()
            .Where(x => x.Id == package.TargetPickupStationId)
            .Select(x => x.SerialNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return new PackageAuditContext(
            package.Id,
            package.OrganizationId,
            package.RecipientCitizenUserId,
            package.CreatedByEmployeeAccountId,
            package.CreatedByOrganizationAdminAccountId,
            package.Description,
            organizationName,
            // Surface the pickup-station serial number rather than the locker number.
            stationSerial,
            claim is null ? null : FormatLocation(claim.Location));
    }

    public async Task<EmployeeAuditContext?> GetEmployeeAsync(
        EmployeeAccountId employeeAccountId,
        CancellationToken cancellationToken = default)
        => await _dbContext.EmployeeAccounts.AsNoTracking()
            .Where(x => x.Id == employeeAccountId)
            .Select(x => new EmployeeAuditContext(x.Id, x.OrganizationId, x.CreatedFromProvisioningCodeId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OrganizationAdminAuditContext?> GetOrganizationAdminAsync(
        OrganizationAdminAccountId accountId,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrganizationAdminAccounts.AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => new OrganizationAdminAuditContext(x.Id, x.OrganizationId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProvisioningCodeAuditContext?> GetProvisioningCodeAsync(
        ProvisioningCodeId provisioningCodeId,
        CancellationToken cancellationToken = default)
        => await _dbContext.ProvisioningCodes.AsNoTracking()
            .Where(x => x.Id == provisioningCodeId)
            .Select(x => new ProvisioningCodeAuditContext(x.Id, x.OrganizationId, x.CreatedByOrganizationAdminId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CitizenActivationCodeAuditContext?> GetCitizenActivationCodeAsync(
        CitizenActivationCodeId codeId,
        CancellationToken cancellationToken = default)
        => await _dbContext.CitizenActivationCodes.AsNoTracking()
            .Where(x => x.Id == codeId)
            .Select(x => new CitizenActivationCodeAuditContext(x.Id, x.CitizenUserId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<StationClaimAuditContext?> GetStationClaimAsync(
        StationClaimId stationClaimId,
        CancellationToken cancellationToken = default)
    {
        var claim = await _dbContext.StationClaims.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == stationClaimId, cancellationToken);
        if (claim is null) return null;

        var stationSerial = await _dbContext.PickupStations.AsNoTracking()
            .Where(x => x.Id == claim.PickupStationId)
            .Select(x => x.SerialNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return new StationClaimAuditContext(
            claim.Id,
            claim.PickupStationId,
            claim.OrganizationId,
            stationSerial,
            FormatLocation(claim.Location));
    }

    public async Task<LockerAuditContext?> GetLockerAsync(
        PickupStationId pickupStationId,
        LockerId lockerId,
        CancellationToken cancellationToken = default)
    {
        var locker = await _dbContext.Lockers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == lockerId && x.PickupStationId == pickupStationId, cancellationToken);
        if (locker is null) return null;

        var claim = await _dbContext.StationClaims.AsNoTracking()
            .Where(x => x.PickupStationId == pickupStationId && x.ReleasedAt == null)
            .OrderByDescending(x => x.ClaimedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new LockerAuditContext(
            pickupStationId,
            lockerId,
            locker.LockerNumber,
            claim?.OrganizationId,
            $"Paketnik #{locker.LockerNumber}",
            claim is null ? null : FormatLocation(claim.Location));
    }

    public async Task<OrganizationAuditContext?> GetOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => await _dbContext.Organizations.AsNoTracking()
            .Where(x => x.Id == organizationId)
            .Select(x => new OrganizationAuditContext(x.Id, x.Name))
            .SingleOrDefaultAsync(cancellationToken);

    private static string FormatLocation(Location location)
        => $"{location.Address} {location.HouseNumber}, {location.ZipCode} {location.City}";
}
