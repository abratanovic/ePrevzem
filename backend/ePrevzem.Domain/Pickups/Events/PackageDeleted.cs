using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageDeleted(
    PackageId PackageId,
    OrganizationId OrganizationId,
    EmployeeAccountId? DeletedByEmployeeAccountId,
    OrganizationAdminAccountId? DeletedByOrganizationAdminAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
