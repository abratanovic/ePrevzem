using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Application.Pickups.Insert;

/// <summary>
/// Shared validation for the two-step operator insertion (open + confirm):
/// the actor is an active Operator of the org, the org holds an active claim on
/// the package's target station, and the package is awaiting placement.
/// </summary>
internal static class InsertionGuard
{
    public static async Task<Package> ResolvePlaceablePackageAsync(
        Guid actorEmployeeId,
        OrganizationId organizationId,
        PackageId packageId,
        IEmployeeAccountRepository employeeRepository,
        IPackageRepository packageRepository,
        IStationClaimRepository stationClaimRepository,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(
            new EmployeeAccountId(actorEmployeeId), cancellationToken);
        if (employee is null
            || employee.OrganizationId != organizationId
            || employee.Status != EmployeeAccountStatus.Active
            || !employee.CanOperateLockers)
            throw new InsertionForbiddenException();

        var package = await packageRepository.GetByIdForOrganizationAsync(packageId, organizationId, cancellationToken)
            ?? throw new PickupNotFoundException(packageId.Value);

        if (package.Status != PackageStatus.AwaitingPlacement)
            throw new LockerUnavailableException();

        var claim = await stationClaimRepository.GetActiveClaimForStationAsync(
            package.TargetPickupStationId, cancellationToken);
        if (claim is null || claim.OrganizationId != organizationId)
            throw new InsertionForbiddenException();

        return package;
    }
}
