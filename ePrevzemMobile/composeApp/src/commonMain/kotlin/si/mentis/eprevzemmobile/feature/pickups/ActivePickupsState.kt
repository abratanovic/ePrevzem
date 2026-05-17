package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

enum class ActiveTab { Pickups, History, Profile }

@Immutable
data class ActivePickupsState(
    val userName: String = "",
    val pickups: List<PickupItem> = emptyList(),
    val isRefreshing: Boolean = false,
    val activeTab: ActiveTab = ActiveTab.Pickups,
    val auditLogEntries:List<AuditLogEntry> = emptyList()
)

