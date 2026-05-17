package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

enum class ActiveTab { Pickups, History, Profile }
enum class AuditLogStatus {Opened, Confirmed, Failed}

@Immutable
data class ActivePickupsState(
    val userName: String = "",
    val pickups: List<PickupItem> = emptyList(),
    val isRefreshing: Boolean = false,
    val activeTab: ActiveTab = ActiveTab.Pickups,
    val auditLogEntries:List<AuditLogEntry> = emptyList()
)

data class AuditLogEntry(
    val id:String,
    val documentTitle: String,
    val organization:String,
    val lockerNumber: String,
    val location: String,
    val openedAt: String,
    val status: AuditLogStatus
)
