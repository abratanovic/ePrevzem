package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Immutable

@Immutable
data class OperatorHomeState(
    val userName: String,
    val activeTab: OperatorTab = OperatorTab.Pickups,
    val pendingInsertionCount: Int = 12,
    val inLockerCount: Int = 7,
    val expiredCount: Int = 3,
)

enum class OperatorTab { Pickups, History, Profile }
