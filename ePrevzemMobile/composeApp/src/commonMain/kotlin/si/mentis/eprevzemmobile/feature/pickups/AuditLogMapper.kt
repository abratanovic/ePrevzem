package si.mentis.eprevzemmobile.feature.pickups

import si.mentis.eprevzemmobile.data.logevent.LogAction
import si.mentis.eprevzemmobile.data.logevent.LogEvent

fun LogEvent.toAuditLogEntry(): AuditLogEntry {
    val eventDetails = details

    return AuditLogEntry(
        id = id,
        // Missing details are left null so the card omits the row entirely
        // rather than showing a placeholder.
        documentTitle = eventDetails?.documentTitle?.takeIf { it.isNotBlank() },
        organization = eventDetails?.organizationName?.takeIf { it.isNotBlank() },
        lockerNumber = eventDetails?.lockerLabel?.takeIf { it.isNotBlank() },
        location = eventDetails?.location?.takeIf { it.isNotBlank() },
        openedAt = occurredAt.toAuditLogDisplayTime(),
        badge = action.toAuditLogBadge(),
    )
}

private fun LogAction.toAuditLogBadge(): AuditLogBadge = when (this) {
    LogAction.PackagePickedUpByCitizen,
    LogAction.PackageMarkedPickedUpManually,
    LogAction.DelegationUsedAtPickup,
    -> AuditLogBadge(label = "Prevzeto", tone = AuditLogBadgeTone.Success)

    LogAction.PackagePlaced,
    -> AuditLogBadge(label = "Vloženo", tone = AuditLogBadgeTone.Info)

    LogAction.PackageRemovedByEmployee,
    -> AuditLogBadge(label = "Odstranjeno", tone = AuditLogBadgeTone.Warning)

    LogAction.PackageExpired,
    -> AuditLogBadge(label = "Poteklo", tone = AuditLogBadgeTone.Warning)

    LogAction.PackageCancelled,
    -> AuditLogBadge(label = "Preklicano", tone = AuditLogBadgeTone.Error)

    LogAction.LockerOpened,
    -> AuditLogBadge(label = "Odprto", tone = AuditLogBadgeTone.Info)

    LogAction.ProvisioningCodeIssued,
    -> AuditLogBadge(label = "Koda izdana", tone = AuditLogBadgeTone.Info)

    LogAction.ProvisioningCodeRedeemed,
    -> AuditLogBadge(label = "Koda uporabljena", tone = AuditLogBadgeTone.Success)

    LogAction.CitizenOnboarded,
    -> AuditLogBadge(label = "Registrirano", tone = AuditLogBadgeTone.Success)

    else -> AuditLogBadge(label = "Zabeleženo", tone = AuditLogBadgeTone.Info)
}

private fun String.toAuditLogDisplayTime(): String {
    if (length < 16) return this

    val year = substring(0, 4)
    val month = substring(5, 7).trimStart('0')
    val day = substring(8, 10).trimStart('0')
    val time = substring(11, 16)

    return "$day. $month. $year ob $time"
}
