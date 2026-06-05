using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class EmployeeAccount : AggregateRoot<EmployeeAccountId>
{
    private readonly List<EmployeeAccountRole> _roles = new();
    private readonly List<PickupStationId> _stationAccess = new();
    private readonly List<EmployeeDevice> _devices = new();

    public OrganizationId OrganizationId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public EmployeeAccountStatus Status { get; private set; }
    public ProvisioningCodeId CreatedFromProvisioningCodeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<EmployeeAccountRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<PickupStationId> StationAccess => _stationAccess.AsReadOnly();
    public IReadOnlyCollection<EmployeeDevice> Devices => _devices.AsReadOnly();
    public EmployeeDevice? ActiveDevice => _devices.SingleOrDefault(d => d.IsActive);

    public bool CanManageRecords => _roles.Contains(EmployeeAccountRole.RecordManager);
    public bool CanOperateLockers => _roles.Contains(EmployeeAccountRole.Operator);

    private EmployeeAccount() { }

    public static EmployeeAccount Create(
        EmployeeAccountId id,
        OrganizationId organizationId,
        string firstName,
        string lastName,
        string? email,
        IReadOnlyCollection<EmployeeAccountRole> roles,
        IReadOnlyCollection<PickupStationId> stationAccess,
        ProvisioningCodeId createdFromProvisioningCodeId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (roles is null || roles.Count == 0)
            throw new ArgumentException("At least one role must be granted.", nameof(roles));

        var acc = new EmployeeAccount
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Status = EmployeeAccountStatus.Active,
            CreatedFromProvisioningCodeId = createdFromProvisioningCodeId,
            CreatedAt = now
        };
        acc._roles.AddRange(roles.Distinct());
        acc._stationAccess.AddRange((stationAccess ?? Array.Empty<PickupStationId>()).Distinct());
        acc.Raise(new EmployeeAccountCreated(id, now));
        return acc;
    }

    public void GrantRole(EmployeeAccountRole role, DateTimeOffset now)
    {
        EnsureActive();
        if (_roles.Contains(role)) return;
        _roles.Add(role);
        Raise(new EmployeeAccountRoleGranted(Id, role, now));
    }

    public void RevokeRole(EmployeeAccountRole role, DateTimeOffset now)
    {
        EnsureActive();
        if (!_roles.Contains(role)) return;
        if (_roles.Count == 1)
            throw new InvalidOperationException("Cannot revoke the last role; an account must have at least one role.");
        _roles.Remove(role);
        Raise(new EmployeeAccountRoleRevoked(Id, role, now));
    }

    public void GrantStationAccess(PickupStationId stationId, DateTimeOffset now)
    {
        EnsureActive();
        if (_stationAccess.Contains(stationId)) return;
        _stationAccess.Add(stationId);
        Raise(new EmployeeStationAccessGranted(Id, stationId, now));
    }

    public void RevokeStationAccess(PickupStationId stationId, DateTimeOffset now)
    {
        EnsureActive();
        if (!_stationAccess.Contains(stationId)) return;
        _stationAccess.Remove(stationId);
        Raise(new EmployeeStationAccessRevoked(Id, stationId, now));
    }

    public EmployeeDevice RegisterDevice(
        EmployeeDeviceId id,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset now)
    {
        EnsureActive();

        var existing = ActiveDevice;
        if (existing is not null)
        {
            existing.Revoke(now);
            Raise(new EmployeeDeviceRevoked(Id, existing.Id, now));
        }

        var device = EmployeeDevice.Register(id, Id, publicKey, deviceFingerprint, label, now);
        _devices.Add(device);
        Raise(new EmployeeDeviceRegistered(Id, id, now));
        return device;
    }

    public void RevokeDevice(EmployeeDeviceId deviceId, DateTimeOffset now)
    {
        var device = _devices.SingleOrDefault(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("Employee device not found on this account.");
        device.Revoke(now);
        Raise(new EmployeeDeviceRevoked(Id, deviceId, now));
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        EnsureActive();
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        MustChangePassword = false;
        Raise(new EmployeePasswordChanged(Id, now));
    }

    public void SetInitialPassword(string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
        MustChangePassword = true;
        Raise(new EmployeePasswordChanged(Id, now));
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        Raise(new EmployeeAccountLoggedIn(Id, now));
    }

    public void Disable(DateTimeOffset now)
    {
        if (Status == EmployeeAccountStatus.Disabled)
            throw new InvalidOperationException("Account is already disabled.");
        Status = EmployeeAccountStatus.Disabled;
        Raise(new EmployeeAccountDisabled(Id, now));
    }

    public void Reenable(DateTimeOffset now)
    {
        if (Status == EmployeeAccountStatus.Active)
            throw new InvalidOperationException("Account is already active.");
        Status = EmployeeAccountStatus.Active;
        Raise(new EmployeeAccountReenabled(Id, now));
    }

    private void EnsureActive()
    {
        if (Status == EmployeeAccountStatus.Disabled)
            throw new InvalidOperationException("Cannot modify a disabled employee account.");
    }
}
