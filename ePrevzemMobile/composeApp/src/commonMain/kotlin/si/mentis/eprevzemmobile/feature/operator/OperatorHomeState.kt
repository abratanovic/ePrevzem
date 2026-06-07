package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Immutable

@Immutable
data class OperatorHomeState(
    val userName: String,
    val activeTab: OperatorTab = OperatorTab.Pickups,
    val pendingInsertionCount: Int = 0,
    val inLockerCount: Int = 0,
    val expiredCount: Int = 0,
)

enum class OperatorTab { Pickups, History, Profile }
