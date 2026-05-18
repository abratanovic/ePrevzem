using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record ProvisioningCodeRedeemed(
    ProvisioningCodeId ProvisioningCodeId,
    EmployeeAccountId RedeemedIntoEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
