package si.mentis.eprevzemmobile.feature.pickups

import kotlinx.datetime.Instant
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime
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
    LogAction.PackageCreated,
    -> AuditLogBadge(label = "Ustvarjeno", tone = AuditLogBadgeTone.Info)

    LogAction.PackagePlaced,
    -> AuditLogBadge(label = "Vstavljeno", tone = AuditLogBadgeTone.Info)

    LogAction.PackagePickedUpByCitizen,
    LogAction.PackageMarkedPickedUpManually,
    -> AuditLogBadge(label = "Prevzeto", tone = AuditLogBadgeTone.Success)

    LogAction.DelegationUsedAtPickup,
    -> AuditLogBadge(label = "Prevzeto s pooblastilom", tone = AuditLogBadgeTone.Success)

    LogAction.PackageRemovedByEmployee,
    -> AuditLogBadge(label = "Odstranjeno iz paketomata", tone = AuditLogBadgeTone.Warning)

    LogAction.PackageRetrievedAfterExpiry,
    -> AuditLogBadge(label = "Umaknjeno po poteku", tone = AuditLogBadgeTone.Warning)

    LogAction.PackageExpired,
    -> AuditLogBadge(label = "Poteklo", tone = AuditLogBadgeTone.Warning)

    LogAction.PackageCancelled,
    -> AuditLogBadge(label = "Preklicano", tone = AuditLogBadgeTone.Error)

    LogAction.PackageDeleted,
    -> AuditLogBadge(label = "Izbrisano", tone = AuditLogBadgeTone.Error)

    LogAction.DelegationCreated,
    -> AuditLogBadge(label = "Pooblastilo ustvarjeno", tone = AuditLogBadgeTone.Info)

    LogAction.DelegationRevoked,
    -> AuditLogBadge(label = "Pooblastilo preklicano", tone = AuditLogBadgeTone.Warning)

    LogAction.LockerOpened,
    -> AuditLogBadge(label = "Odprto", tone = AuditLogBadgeTone.Info)

    else -> AuditLogBadge(label = "Zabeleženo", tone = AuditLogBadgeTone.Info)
}

/**
 * Formats a backend ISO-8601 timestamp (UTC / with offset) as `d. M. yyyy ob HH:mm`
 * in the device's local time zone. Falls back to the raw input if it cannot be parsed.
 */
internal fun String.toAuditLogDisplayTime(
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
): String {
    val instant = runCatching { Instant.parse(this) }.getOrNull() ?: return this
    val local = instant.toLocalDateTime(timeZone)
    val hour = local.hour.toString().padStart(2, '0')
    val minute = local.minute.toString().padStart(2, '0')
    return "${local.dayOfMonth}. ${local.monthNumber}. ${local.year} ob $hour:$minute"
}
