using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Common.Events;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Delegations.Events;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers.Events;
using ePrevzem.Domain.Organizations.Events;
using ePrevzem.Domain.Pickups.Events;
using MediatR;

namespace ePrevzem.Application.Audit;

public sealed class AuditDomainEventHandlers :
    INotificationHandler<DomainEventNotification<PackageCreated>>,
    INotificationHandler<DomainEventNotification<PackagePlaced>>,
    INotificationHandler<DomainEventNotification<PackagePickedUpByCitizen>>,
    INotificationHandler<DomainEventNotification<PackageRemovedByEmployee>>,
    INotificationHandler<DomainEventNotification<PackageExpired>>,
    INotificationHandler<DomainEventNotification<PackageRetrievedAfterExpiry>>,
    INotificationHandler<DomainEventNotification<PackageMarkedPickedUpManually>>,
    INotificationHandler<DomainEventNotification<PackageCancelled>>,
    INotificationHandler<DomainEventNotification<PackageDeleted>>,
    INotificationHandler<DomainEventNotification<ProvisioningCodeIssued>>,
    INotificationHandler<DomainEventNotification<ProvisioningCodeRedeemed>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountCreated>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountDisabled>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountReenabled>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountRoleGranted>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountRoleRevoked>>,
    INotificationHandler<DomainEventNotification<EmployeeStationAccessGranted>>,
    INotificationHandler<DomainEventNotification<EmployeeStationAccessRevoked>>,
    INotificationHandler<DomainEventNotification<EmployeeDeviceRegistered>>,
    INotificationHandler<DomainEventNotification<EmployeeDeviceRevoked>>,
    INotificationHandler<DomainEventNotification<EmployeePasswordChanged>>,
    INotificationHandler<DomainEventNotification<EmployeeAccountLoggedIn>>,
    INotificationHandler<DomainEventNotification<CitizenActivationCodeIssued>>,
    INotificationHandler<DomainEventNotification<CitizenDeviceRegistered>>,
    INotificationHandler<DomainEventNotification<CitizenDeviceRevoked>>,
    INotificationHandler<DomainEventNotification<CitizenOnboarded>>,
    INotificationHandler<DomainEventNotification<OrganizationAdminAccountCreated>>,
    INotificationHandler<DomainEventNotification<OrganizationAdminAccountDisabled>>,
    INotificationHandler<DomainEventNotification<OrganizationAdminAccountReenabled>>,
    INotificationHandler<DomainEventNotification<OrganizationAdminLoggedIn>>,
    INotificationHandler<DomainEventNotification<OrganizationAdminPasswordChanged>>,
    INotificationHandler<DomainEventNotification<SystemAdminLoggedIn>>,
    INotificationHandler<DomainEventNotification<SystemAdminLoginFailed>>,
    INotificationHandler<DomainEventNotification<SystemAdminPasswordChanged>>,
    INotificationHandler<DomainEventNotification<RefreshTokenRotated>>,
    INotificationHandler<DomainEventNotification<RefreshTokenChainRevoked>>,
    INotificationHandler<DomainEventNotification<OrganizationCreated>>,
    INotificationHandler<DomainEventNotification<PickupStationCreated>>,
    INotificationHandler<DomainEventNotification<LockerCreated>>,
    INotificationHandler<DomainEventNotification<LockerServiceabilityChanged>>,
    INotificationHandler<DomainEventNotification<StationClaimed>>,
    INotificationHandler<DomainEventNotification<StationReleased>>,
    INotificationHandler<DomainEventNotification<DelegationCreated>>,
    INotificationHandler<DomainEventNotification<DelegationRevoked>>
{
    private readonly AuditLogWriter _writer;
    private readonly IAuditContextLookup _lookup;

    public AuditDomainEventHandlers(AuditLogWriter writer, IAuditContextLookup lookup)
    {
        _writer = writer;
        _lookup = lookup;
    }

    public async Task Handle(DomainEventNotification<PackageCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        if (ctx is null) return;

        await _writer.RecordAsync(
            ev,
            ctx.CreatedByOrganizationAdminAccountId is not null
                ? AuditActor.OrganizationAdmin(ctx.CreatedByOrganizationAdminAccountId.Value)
                : AuditActor.Employee(ctx.CreatedByEmployeeAccountId!.Value),
            ctx.OrganizationId,
            AuditAction.PackageCreated,
            AuditTargetKind.Package,
            ev.PackageId.Value,
            Details(ctx),
            cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackagePlaced> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await _writer.RecordAsync(
            ev,
            AuditActor.Employee(ev.OpenedByEmployeeAccountId),
            ctx?.OrganizationId,
            AuditAction.PackagePlaced,
            AuditTargetKind.Package,
            ev.PackageId.Value,
            Details(ctx, $"Locker {ev.LockerId.Value}", null),
            cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackagePickedUpByCitizen> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await RecordPackageAsync(ev, AuditActor.Citizen(ev.PickedUpByCitizenUserId), ctx, AuditAction.PackagePickedUpByCitizen, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackageRemovedByEmployee> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await RecordPackageAsync(ev, AuditActor.Employee(ev.RemovedByEmployeeAccountId), ctx, AuditAction.PackageRemovedByEmployee, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackageExpired> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await RecordPackageAsync(ev, AuditActor.System(), ctx, AuditAction.PackageExpired, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackageRetrievedAfterExpiry> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await RecordPackageAsync(ev, AuditActor.Employee(ev.RetrievedByEmployeeAccountId), ctx, AuditAction.PackageRetrievedAfterExpiry, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackageMarkedPickedUpManually> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        await RecordPackageAsync(ev, AuditActor.Employee(ev.MarkedByEmployeeAccountId), ctx, AuditAction.PackageMarkedPickedUpManually, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<PackageCancelled> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetPackageAsync(ev.PackageId, cancellationToken);
        var actor = ev.CancelledByOrganizationAdminAccountId is not null
            ? AuditActor.OrganizationAdmin(ev.CancelledByOrganizationAdminAccountId.Value)
            : AuditActor.Employee(ev.CancelledByEmployeeAccountId!.Value);
        await RecordPackageAsync(ev, actor, ctx, AuditAction.PackageCancelled, cancellationToken);
    }

    public Task Handle(DomainEventNotification<PackageDeleted> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var actor = ev.DeletedByOrganizationAdminAccountId is not null
            ? AuditActor.OrganizationAdmin(ev.DeletedByOrganizationAdminAccountId.Value)
            : AuditActor.Employee(ev.DeletedByEmployeeAccountId!.Value);
        return _writer.RecordAsync(
            ev,
            actor,
            ev.OrganizationId,
            AuditAction.PackageDeleted,
            AuditTargetKind.Package,
            ev.PackageId.Value,
            null,
            cancellationToken);
    }

    public async Task Handle(DomainEventNotification<ProvisioningCodeIssued> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetProvisioningCodeAsync(ev.ProvisioningCodeId, cancellationToken);
        if (ctx is null) return;
        await _writer.RecordAsync(ev, AuditActor.OrganizationAdmin(ctx.CreatedByOrganizationAdminId), ctx.OrganizationId,
            AuditAction.ProvisioningCodeIssued, AuditTargetKind.ProvisioningCode, ev.ProvisioningCodeId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<ProvisioningCodeRedeemed> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetProvisioningCodeAsync(ev.ProvisioningCodeId, cancellationToken);
        await _writer.RecordAsync(ev, AuditActor.Employee(ev.RedeemedIntoEmployeeAccountId), ctx?.OrganizationId,
            AuditAction.ProvisioningCodeRedeemed, AuditTargetKind.ProvisioningCode, ev.ProvisioningCodeId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<EmployeeAccountCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var employee = await _lookup.GetEmployeeAsync(ev.EmployeeAccountId, cancellationToken);
        if (employee is null) return;
        var provisioning = await _lookup.GetProvisioningCodeAsync(employee.CreatedFromProvisioningCodeId, cancellationToken);
        await _writer.RecordAsync(ev, provisioning is null ? _writer.CurrentActorOrSystem() : AuditActor.OrganizationAdmin(provisioning.CreatedByOrganizationAdminId),
            employee.OrganizationId, AuditAction.EmployeeAccountCreated, AuditTargetKind.EmployeeAccount, ev.EmployeeAccountId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<EmployeeAccountDisabled> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeAccountDisabled, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeAccountReenabled> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeAccountReenabled, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeAccountRoleGranted> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeAccountRoleGranted, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeAccountRoleRevoked> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeAccountRoleRevoked, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeStationAccessGranted> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeStationAccessGranted, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeStationAccessRevoked> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeeStationAccessRevoked, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeDeviceRegistered> notification, CancellationToken cancellationToken)
        => await RecordEmployeeDeviceAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, notification.DomainEvent.EmployeeDeviceId.Value, AuditAction.EmployeeDeviceRegistered, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeDeviceRevoked> notification, CancellationToken cancellationToken)
        => await RecordEmployeeDeviceAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, notification.DomainEvent.EmployeeDeviceId.Value, AuditAction.EmployeeDeviceRevoked, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeePasswordChanged> notification, CancellationToken cancellationToken)
        => await RecordEmployeeAsync(notification.DomainEvent, notification.DomainEvent.EmployeeAccountId, AuditAction.EmployeePasswordChanged, cancellationToken);

    public async Task Handle(DomainEventNotification<EmployeeAccountLoggedIn> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetEmployeeAsync(ev.EmployeeAccountId, cancellationToken);
        await _writer.RecordAsync(ev, AuditActor.Employee(ev.EmployeeAccountId), ctx?.OrganizationId,
            AuditAction.EmployeeAccountLoggedIn, AuditTargetKind.EmployeeAccount, ev.EmployeeAccountId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<CitizenActivationCodeIssued> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        await _writer.RecordAsync(ev, AuditActor.Citizen(ev.CitizenUserId), null,
            AuditAction.CitizenActivationCodeIssued, AuditTargetKind.CitizenActivationCode, ev.Id.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<CitizenDeviceRegistered> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.Citizen(ev.CitizenUserId), null,
            AuditAction.CitizenDeviceRegistered, AuditTargetKind.CitizenDevice, ev.CitizenDeviceId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<CitizenDeviceRevoked> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.Citizen(ev.CitizenUserId), null,
            AuditAction.CitizenDeviceRevoked, AuditTargetKind.CitizenDevice, ev.CitizenDeviceId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<CitizenOnboarded> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.Citizen(ev.CitizenUserId), null,
            AuditAction.CitizenOnboarded, AuditTargetKind.CitizenUser, ev.CitizenUserId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<OrganizationAdminAccountCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ev.OrganizationId,
            AuditAction.OrganizationAdminAccountCreated, AuditTargetKind.OrganizationAdminAccount, ev.OrganizationAdminAccountId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<OrganizationAdminAccountDisabled> notification, CancellationToken cancellationToken)
        => await RecordOrganizationAdminAsync(notification.DomainEvent, notification.DomainEvent.OrganizationAdminAccountId, AuditAction.OrganizationAdminAccountDisabled, cancellationToken);

    public async Task Handle(DomainEventNotification<OrganizationAdminAccountReenabled> notification, CancellationToken cancellationToken)
        => await RecordOrganizationAdminAsync(notification.DomainEvent, notification.DomainEvent.OrganizationAdminAccountId, AuditAction.OrganizationAdminAccountReenabled, cancellationToken);

    public async Task Handle(DomainEventNotification<OrganizationAdminLoggedIn> notification, CancellationToken cancellationToken)
        => await RecordOrganizationAdminSelfAsync(notification.DomainEvent, notification.DomainEvent.OrganizationAdminAccountId, AuditAction.OrganizationAdminLoggedIn, cancellationToken);

    public async Task Handle(DomainEventNotification<OrganizationAdminPasswordChanged> notification, CancellationToken cancellationToken)
        => await RecordOrganizationAdminSelfAsync(notification.DomainEvent, notification.DomainEvent.OrganizationAdminAccountId, AuditAction.OrganizationAdminPasswordChanged, cancellationToken);

    public Task Handle(DomainEventNotification<SystemAdminLoggedIn> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.SystemAdmin(ev.SystemAdminId), null,
            AuditAction.SystemAdminLoggedIn, AuditTargetKind.SystemAdmin, ev.SystemAdminId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<SystemAdminLoginFailed> notification, CancellationToken cancellationToken)
        => _writer.RecordAsync(notification.DomainEvent, AuditActor.System(), null,
            AuditAction.SystemAdminLoginFailed, AuditTargetKind.SystemAdmin, Guid.Empty, null, cancellationToken);

    public Task Handle(DomainEventNotification<SystemAdminPasswordChanged> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.SystemAdmin(ev.SystemAdminId), null,
            AuditAction.SystemAdminPasswordChanged, AuditTargetKind.SystemAdmin, ev.SystemAdminId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<RefreshTokenRotated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, OwnerActor(ev.SystemAdminId, ev.OrganizationAdminAccountId, ev.EmployeeAccountId, ev.CitizenUserId), null,
            AuditAction.RefreshTokenRotated, AuditTargetKind.RefreshToken, ev.OldTokenId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<RefreshTokenChainRevoked> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, OwnerActor(ev.SystemAdminId, ev.OrganizationAdminAccountId, ev.EmployeeAccountId, ev.CitizenUserId), null,
            AuditAction.RefreshTokenChainRevoked, AuditTargetKind.RefreshToken, ev.TriggerTokenId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<OrganizationCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var org = await _lookup.GetOrganizationAsync(ev.OrganizationId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ev.OrganizationId,
            AuditAction.OrganizationCreated, AuditTargetKind.Organization, ev.OrganizationId.Value,
            org is null ? null : new AuditLogDetailsResponse(OrganizationName: org.Name), cancellationToken);
    }

    public Task Handle(DomainEventNotification<PickupStationCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), null,
            AuditAction.PickupStationCreated, AuditTargetKind.PickupStation, ev.PickupStationId.Value, null, cancellationToken);
    }

    public async Task Handle(DomainEventNotification<LockerCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetLockerAsync(ev.PickupStationId, ev.LockerId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ctx?.OrganizationId,
            AuditAction.LockerCreated, AuditTargetKind.Locker, ev.LockerId.Value,
            ctx is null ? null : new AuditLogDetailsResponse(LockerLabel: ctx.LockerLabel, Location: ctx.Location), cancellationToken);
    }

    public async Task Handle(DomainEventNotification<LockerServiceabilityChanged> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetLockerAsync(ev.PickupStationId, ev.LockerId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ctx?.OrganizationId,
            AuditAction.LockerServiceabilityChanged, AuditTargetKind.Locker, ev.LockerId.Value,
            ctx is null ? null : new AuditLogDetailsResponse(LockerLabel: ctx.LockerLabel, Location: ctx.Location), cancellationToken);
    }

    public async Task Handle(DomainEventNotification<StationClaimed> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetStationClaimAsync(ev.StationClaimId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ev.OrganizationId,
            AuditAction.StationClaimed, AuditTargetKind.StationClaim, ev.StationClaimId.Value,
            ctx is null ? null : new AuditLogDetailsResponse(LockerLabel: ctx.LockerLabel, Location: ctx.Location), cancellationToken);
    }

    public async Task Handle(DomainEventNotification<StationReleased> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        var ctx = await _lookup.GetStationClaimAsync(ev.StationClaimId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ev.OrganizationId,
            AuditAction.StationReleased, AuditTargetKind.StationClaim, ev.StationClaimId.Value,
            ctx is null ? null : new AuditLogDetailsResponse(LockerLabel: ctx.LockerLabel, Location: ctx.Location), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DelegationCreated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, AuditActor.Citizen(ev.DelegatorCitizenUserId), null,
            AuditAction.DelegationCreated, AuditTargetKind.Delegation, ev.DelegationId.Value, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<DelegationRevoked> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), null,
            AuditAction.DelegationRevoked, AuditTargetKind.Delegation, ev.DelegationId.Value, null, cancellationToken);
    }

    private Task RecordPackageAsync(
        ePrevzem.Domain.Common.IDomainEvent ev,
        AuditActor actor,
        PackageAuditContext? ctx,
        AuditAction action,
        CancellationToken cancellationToken)
        => _writer.RecordAsync(ev, actor, ctx?.OrganizationId, action, AuditTargetKind.Package,
            GetPackageId(ev).Value, Details(ctx), cancellationToken);

    private async Task RecordEmployeeAsync(
        ePrevzem.Domain.Common.IDomainEvent ev,
        ePrevzem.Domain.Identity.EmployeeAccountId employeeId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var ctx = await _lookup.GetEmployeeAsync(employeeId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ctx?.OrganizationId,
            action, AuditTargetKind.EmployeeAccount, employeeId.Value, null, cancellationToken);
    }

    private async Task RecordEmployeeDeviceAsync(
        ePrevzem.Domain.Common.IDomainEvent ev,
        ePrevzem.Domain.Identity.EmployeeAccountId employeeId,
        Guid deviceId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var ctx = await _lookup.GetEmployeeAsync(employeeId, cancellationToken);
        await _writer.RecordAsync(ev, AuditActor.Employee(employeeId), ctx?.OrganizationId,
            action, AuditTargetKind.EmployeeDevice, deviceId, null, cancellationToken);
    }

    private async Task RecordOrganizationAdminAsync(
        ePrevzem.Domain.Common.IDomainEvent ev,
        ePrevzem.Domain.Identity.OrganizationAdminAccountId accountId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var ctx = await _lookup.GetOrganizationAdminAsync(accountId, cancellationToken);
        await _writer.RecordAsync(ev, _writer.CurrentActorOrSystem(), ctx?.OrganizationId,
            action, AuditTargetKind.OrganizationAdminAccount, accountId.Value, null, cancellationToken);
    }

    private async Task RecordOrganizationAdminSelfAsync(
        ePrevzem.Domain.Common.IDomainEvent ev,
        ePrevzem.Domain.Identity.OrganizationAdminAccountId accountId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var ctx = await _lookup.GetOrganizationAdminAsync(accountId, cancellationToken);
        await _writer.RecordAsync(ev, AuditActor.OrganizationAdmin(accountId), ctx?.OrganizationId,
            action, AuditTargetKind.OrganizationAdminAccount, accountId.Value, null, cancellationToken);
    }

    private static AuditActor OwnerActor(
        ePrevzem.Domain.Identity.SystemAdminId? systemAdminId,
        ePrevzem.Domain.Identity.OrganizationAdminAccountId? organizationAdminAccountId,
        ePrevzem.Domain.Identity.EmployeeAccountId? employeeAccountId,
        ePrevzem.Domain.Identity.CitizenUserId? citizenUserId)
    {
        if (systemAdminId is not null) return AuditActor.SystemAdmin(systemAdminId.Value);
        if (organizationAdminAccountId is not null) return AuditActor.OrganizationAdmin(organizationAdminAccountId.Value);
        if (employeeAccountId is not null) return AuditActor.Employee(employeeAccountId.Value);
        if (citizenUserId is not null) return AuditActor.Citizen(citizenUserId.Value);
        return AuditActor.System();
    }

    private static ePrevzem.Domain.Pickups.PackageId GetPackageId(ePrevzem.Domain.Common.IDomainEvent ev)
        => ev switch
        {
            PackagePickedUpByCitizen x => x.PackageId,
            PackageRemovedByEmployee x => x.PackageId,
            PackageExpired x => x.PackageId,
            PackageRetrievedAfterExpiry x => x.PackageId,
            PackageMarkedPickedUpManually x => x.PackageId,
            PackageCancelled x => x.PackageId,
            _ => throw new InvalidOperationException($"Domain event '{ev.GetType().Name}' does not carry a package id.")
        };

    private static AuditLogDetailsResponse? Details(
        PackageAuditContext? ctx,
        string? fallbackLockerLabel = null,
        string? fallbackLocation = null)
        => ctx is null
            ? null
            : new AuditLogDetailsResponse(
                DocumentTitle: ctx.Description,
                OrganizationName: ctx.OrganizationName,
                LockerLabel: ctx.LockerLabel ?? fallbackLockerLabel,
                Location: ctx.Location ?? fallbackLocation);
}
