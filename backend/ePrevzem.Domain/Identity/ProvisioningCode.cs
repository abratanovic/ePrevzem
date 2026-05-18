using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class ProvisioningCode : AggregateRoot<ProvisioningCodeId>
{
    private readonly List<EmployeeAccountRole> _roles = new();
    private readonly List<PickupStationId> _stationAccess = new();

    public OrganizationId OrganizationId { get; private set; }
    public string Code { get; private set; } = default!;
    public string PreFilledFirstName { get; private set; } = default!;
    public string PreFilledLastName { get; private set; } = default!;
    public string? PreFilledEmail { get; private set; }
    public IReadOnlyCollection<EmployeeAccountRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<PickupStationId> StationAccess => _stationAccess.AsReadOnly();
    public EmployeeAccountId CreatedByEmployeeAccountId { get; private set; }
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
        string preFilledFirstName,
        string preFilledLastName,
        string? preFilledEmail,
        IReadOnlyCollection<EmployeeAccountRole> roles,
        IReadOnlyCollection<PickupStationId> stationAccess,
        EmployeeAccountId createdBy,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        EmployeeAccountId? isReprovisioningOf)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(preFilledFirstName))
            throw new ArgumentException("First name is required.", nameof(preFilledFirstName));
        if (string.IsNullOrWhiteSpace(preFilledLastName))
            throw new ArgumentException("Last name is required.", nameof(preFilledLastName));
        if (roles is null || roles.Count == 0)
            throw new ArgumentException("At least one role must be granted.", nameof(roles));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        var pc = new ProvisioningCode
        {
            Id = id,
            OrganizationId = organizationId,
            Code = code,
            PreFilledFirstName = preFilledFirstName,
            PreFilledLastName = preFilledLastName,
            PreFilledEmail = preFilledEmail,
            CreatedByEmployeeAccountId = createdBy,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsReprovisioningOfEmployeeAccountId = isReprovisioningOf
        };
        pc._roles.AddRange(roles.Distinct());
        pc._stationAccess.AddRange(stationAccess?.Distinct() ?? Array.Empty<PickupStationId>());
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
