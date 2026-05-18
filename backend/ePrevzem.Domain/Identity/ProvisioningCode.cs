using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class ProvisioningCode : AggregateRoot<ProvisioningCodeId>
{
    private readonly List<EmployeeAccountRole> _roles = new();

    public OrganizationId OrganizationId { get; private set; }
    public string Code { get; private set; } = default!;
    public PersonalInfo PreFilledInfo { get; private set; } = default!;
    public IReadOnlyCollection<EmployeeAccountRole> Roles => _roles.AsReadOnly();
    public OrganizationAdminAccountId CreatedByOrganizationAdminId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public EmployeeAccountId? RedeemedIntoEmployeeAccountId { get; private set; }
    public EmployeeAccountId? IsReprovisioningOfEmployeeAccountId { get; private set; }

    private ProvisioningCode() { }

    public static ProvisioningCode Issue(
        ProvisioningCodeId id,
        OrganizationId organizationId,
        string code,
        PersonalInfo preFilledInfo,
        IReadOnlyCollection<EmployeeAccountRole> roles,
        OrganizationAdminAccountId createdBy,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        EmployeeAccountId? isReprovisioningOf)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (roles is null || roles.Count == 0)
            throw new ArgumentException("At least one role must be granted.", nameof(roles));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        var pc = new ProvisioningCode
        {
            Id = id,
            OrganizationId = organizationId,
            Code = code,
            PreFilledInfo = preFilledInfo,
            CreatedByOrganizationAdminId = createdBy,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsReprovisioningOfEmployeeAccountId = isReprovisioningOf
        };
        pc._roles.AddRange(roles.Distinct());
        pc.Raise(new ProvisioningCodeIssued(id, now));
        return pc;
    }

    public bool IsRedeemable(DateTimeOffset at) => RedeemedAt is null && at < ExpiresAt;

    public void Redeem(DateTimeOffset redeemedAt, EmployeeAccountId redeemedIntoEmployeeAccountId)
    {
        if (RedeemedAt is not null)
            throw new InvalidOperationException("Code is already redeemed.");
        if (redeemedAt >= ExpiresAt)
            throw new InvalidOperationException("Code has expired.");

        RedeemedAt = redeemedAt;
        RedeemedIntoEmployeeAccountId = redeemedIntoEmployeeAccountId;
        Raise(new ProvisioningCodeRedeemed(Id, redeemedIntoEmployeeAccountId, redeemedAt));
    }
}
