package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Immutable

@Immutable
data class OperatorHomeState(
    val userName: String,
    val activeTab: OperatorTab = OperatorTab.Pickups,
)

enum class OperatorTab { Pickups, History, Profile }
