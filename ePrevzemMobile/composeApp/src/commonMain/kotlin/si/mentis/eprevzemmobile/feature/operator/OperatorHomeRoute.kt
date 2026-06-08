package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.dashboard.DashboardRepository
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.feature.pickups.HistoryContent
import si.mentis.eprevzemmobile.feature.profile.ProfileContent

@Composable
fun OperatorHomeRoute(
    user: AppUser.Employee,
    onScanQrClicked: () -> Unit,
    onAddAccount: () -> Unit = {},
    onSwitchAccount: (String) -> Unit = {},
    onUserUpdated: (AppUser) -> Unit = {},
    dashboardRepository: DashboardRepository = AppContainer.dashboardRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(OperatorHomeState(userName = user.fullName)) }

    LaunchedEffect(user.id) {
        dashboardRepository.getStats()?.let { stats ->
            state = state.copy(
                pendingInsertionCount = stats.pendingInsertionCount,
                inLockerCount = stats.inLockerCount,
                expiredCount = stats.expiredCount,
            )
        }
    }

    OperatorHomeScreen(
        state = state,
        modifier = modifier,
        historyContent = { HistoryContent(user = user) },
        profileContent = {
            ProfileContent(
                user = user,
                onAddAccount = onAddAccount,
                onSwitchAccount = onSwitchAccount,
                onUserUpdated = onUserUpdated,
            )
        },
        onEvent = { event ->
            when (event) {
                OperatorHomeEvent.ScanQrClicked -> onScanQrClicked()
                is OperatorHomeEvent.TabSelected -> state = state.copy(activeTab = event.tab)
            }
        },
    )
}
