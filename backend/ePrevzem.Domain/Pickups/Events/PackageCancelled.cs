using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageCancelled(
    PackageId PackageId,
    EmployeeAccountId? CancelledByEmployeeAccountId,
    OrganizationAdminAccountId? CancelledByOrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
