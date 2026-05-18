using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class EmployeeAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly byte[] AnyKey = new byte[] { 9, 9, 9 };

    [Fact]
    public void Create_constructs_account_with_roles_and_station_access()
    {
        var id = EmployeeAccountId.New();
        var orgId = OrganizationId.New();
        var stationId = PickupStationId.New();
        var codeId = ProvisioningCodeId.New();

        var acc = EmployeeAccount.Create(
            id,
            orgId,
            "Ana",
            "Kovač",
            "ana@example.com",
            new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager },
            new[] { stationId },
            codeId,
            Now);

        acc.Id.Should().Be(id);
        acc.OrganizationId.Should().Be(orgId);
        acc.FirstName.Should().Be("Ana");
        acc.LastName.Should().Be("Kovač");
        acc.Email.Should().Be("ana@example.com");
        acc.Status.Should().Be(EmployeeAccountStatus.Active);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
        acc.StationAccess.Should().BeEquivalentTo(new[] { stationId });
        acc.CreatedFromProvisioningCodeId.Should().Be(codeId);
        acc.CreatedAt.Should().Be(Now);
        acc.Devices.Should().BeEmpty();
        acc.DomainEvents.OfType<EmployeeAccountCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_with_empty_roles_throws()
    {
        var act = () => EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(), "F", "L", null,
            Array.Empty<EmployeeAccountRole>(), Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("roles");
    }

    [Fact]
    public void OrganizationAdmin_implies_record_and_operator_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.OrganizationAdmin });
        acc.CanManageRecords.Should().BeTrue();
        acc.CanOperateLockers.Should().BeTrue();
        acc.CanManageOrgAndEmployees.Should().BeTrue();
    }

    [Fact]
    public void RecordManager_only_grants_record_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.RecordManager });
        acc.CanManageRecords.Should().BeTrue();
        acc.CanOperateLockers.Should().BeFalse();
        acc.CanManageOrgAndEmployees.Should().BeFalse();
    }

    [Fact]
    public void Operator_only_grants_operator_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.CanManageRecords.Should().BeFalse();
        acc.CanOperateLockers.Should().BeTrue();
        acc.CanManageOrgAndEmployees.Should().BeFalse();
    }

    [Fact]
    public void GrantRole_adds_role_idempotently()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.GrantRole(EmployeeAccountRole.RecordManager);
        acc.GrantRole(EmployeeAccountRole.RecordManager);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
    }

    [Fact]
    public void RevokeRole_removes_role()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
        acc.RevokeRole(EmployeeAccountRole.RecordManager);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator });
    }

    [Fact]
    public void RevokeRole_below_minimum_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var act = () => acc.RevokeRole(EmployeeAccountRole.Operator);
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one role*");
    }

    [Fact]
    public void GrantStationAccess_adds_idempotently()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var s = PickupStationId.New();
        acc.GrantStationAccess(s);
        acc.GrantStationAccess(s);
        acc.StationAccess.Should().ContainSingle(x => x == s);
    }

    [Fact]
    public void RevokeStationAccess_removes_entry()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var s = PickupStationId.New();
        acc.GrantStationAccess(s);
        acc.RevokeStationAccess(s);
        acc.StationAccess.Should().BeEmpty();
    }

    [Fact]
    public void RegisterDevice_adds_active_device_when_none_exists()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var d = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now);

        d.IsActive.Should().BeTrue();
        acc.ActiveDevice.Should().BeSameAs(d);
        acc.DomainEvents.OfType<EmployeeDeviceRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void RegisterDevice_auto_revokes_previous_active_device()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var old = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp1", null, Now);
        var fresh = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp2", null, Now.AddDays(1));

        old.IsActive.Should().BeFalse();
        old.RevokedAt.Should().Be(Now.AddDays(1));
        fresh.IsActive.Should().BeTrue();
        acc.ActiveDevice.Should().BeSameAs(fresh);
        acc.DomainEvents.OfType<EmployeeDeviceRegistered>().Should().HaveCount(2);
        acc.DomainEvents.OfType<EmployeeDeviceRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void RevokeDevice_marks_revoked_and_clears_active_device()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var d = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now);
        acc.RevokeDevice(d.Id, Now.AddDays(1));

        d.IsActive.Should().BeFalse();
        acc.ActiveDevice.Should().BeNull();
    }

    [Fact]
    public void RevokeDevice_unknown_id_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var act = () => acc.RevokeDevice(EmployeeDeviceId.New(), Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Disable_then_Reenable_toggles_status_and_raises_events()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));
        acc.Status.Should().Be(EmployeeAccountStatus.Disabled);
        acc.Reenable(Now.AddDays(2));
        acc.Status.Should().Be(EmployeeAccountStatus.Active);

        acc.DomainEvents.OfType<EmployeeAccountDisabled>().Should().ContainSingle();
        acc.DomainEvents.OfType<EmployeeAccountReenabled>().Should().ContainSingle();
    }

    [Fact]
    public void Disable_when_already_disabled_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));
        var act = () => acc.Disable(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Operations_on_disabled_account_throw()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));

        var grantRole = () => acc.GrantRole(EmployeeAccountRole.RecordManager);
        var revokeRole = () => acc.RevokeRole(EmployeeAccountRole.Operator);
        var grantStation = () => acc.GrantStationAccess(PickupStationId.New());
        var revokeStation = () => acc.RevokeStationAccess(PickupStationId.New());
        var registerDevice = () => acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now.AddDays(2));

        grantRole.Should().Throw<InvalidOperationException>();
        revokeRole.Should().Throw<InvalidOperationException>();
        grantStation.Should().Throw<InvalidOperationException>();
        revokeStation.Should().Throw<InvalidOperationException>();
        registerDevice.Should().Throw<InvalidOperationException>();
    }

    private static EmployeeAccount Account(IReadOnlyCollection<EmployeeAccountRole> roles)
        => EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(), "Ana", "Kovač", null,
            roles, Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
}
