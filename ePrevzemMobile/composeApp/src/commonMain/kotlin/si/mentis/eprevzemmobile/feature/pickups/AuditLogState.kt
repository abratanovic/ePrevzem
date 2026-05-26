package si.mentis.eprevzemmobile.feature.pickups

enum class AuditLogBadgeTone {
    Info,
    Success,
    Warning,
    Error,
}

data class AuditLogBadge(
    val label: String,
    val tone: AuditLogBadgeTone,
)

data class AuditLogEntry(
    val id:String,
    val documentTitle: String,
    val organization:String,
    val lockerNumber: String,
    val location: String,
    val openedAt: String,
    val badge: AuditLogBadge,
)
