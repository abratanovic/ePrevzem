package si.mentis.eprevzemmobile.feature.pickups

enum class AuditLogStatus {Opened, Confirmed, Failed}

data class AuditLogEntry(
    val id:String,
    val documentTitle: String,
    val organization:String,
    val lockerNumber: String,
    val location: String,
    val openedAt: String,
    val status: AuditLogStatus
)