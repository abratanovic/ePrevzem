package si.mentis.eprevzemmobile.feature.pickups

import si.mentis.eprevzemmobile.data.logevent.LogAction
import si.mentis.eprevzemmobile.data.logevent.LogEvent

fun LogEvent.toAuditLogEntry(): AuditLogEntry {
    val eventDetails = details

    return AuditLogEntry(
        id = id,
        documentTitle = eventDetails?.documentTitle ?: "Dokument",
        organization = eventDetails?.organizationName ?: "Organizacija",
        lockerNumber = eventDetails?.lockerLabel ?: "Paketnik",
        location = eventDetails?.location ?: "",
        openedAt = occurredAt.toAuditLogDisplayTime(),
        status = action.toAuditLogStatus(),
    )
}

private fun LogAction.toAuditLogStatus(): AuditLogStatus = when (this) {
    LogAction.PackagePickedUpByCitizen,
    LogAction.PackageMarkedPickedUpManually,
    LogAction.DelegationUsedAtPickup,
    -> AuditLogStatus.Confirmed

    LogAction.LockerOpened,
    LogAction.PackagePlaced,
    -> AuditLogStatus.Opened

    LogAction.PackageExpired,
    LogAction.PackageCancelled,
    LogAction.PackageRemovedByEmployee,
    -> AuditLogStatus.Failed

    else -> AuditLogStatus.Opened
}

private fun String.toAuditLogDisplayTime(): String {
    if (length < 16) return this

    val year = substring(0, 4)
    val month = substring(5, 7).trimStart('0')
    val day = substring(8, 10).trimStart('0')
    val time = substring(11, 16)

    return "$day. $month. $year ob $time"
}
