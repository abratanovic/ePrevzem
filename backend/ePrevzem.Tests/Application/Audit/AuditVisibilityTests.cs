using ePrevzem.Application.Audit;
using ePrevzem.Domain.Audit;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Audit;

public class AuditVisibilityTests
{
    [Fact]
    public void CitizenActions_are_exactly_the_package_and_delegation_events()
    {
        AuditVisibility.CitizenActions.Should().BeEquivalentTo(new[]
        {
            AuditAction.PackageCreated,
            AuditAction.PackagePlaced,
            AuditAction.PackagePickedUpByCitizen,
            AuditAction.PackageRemovedByEmployee,
            AuditAction.PackageExpired,
            AuditAction.PackageRetrievedAfterExpiry,
            AuditAction.PackageMarkedPickedUpManually,
            AuditAction.PackageCancelled,
            AuditAction.PackageDeleted,
            AuditAction.DelegationCreated,
            AuditAction.DelegationRevoked,
            AuditAction.DelegationUsedAtPickup,
        });
    }
}
