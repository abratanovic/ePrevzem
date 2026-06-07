package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.domain.AppUser

@Composable
fun OperatorHomeRoute(
    user: AppUser.Employee,
    onScanQrClicked: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(OperatorHomeState(userName = user.fullName)) }

    OperatorHomeScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                OperatorHomeEvent.ScanQrClicked -> onScanQrClicked()
                is OperatorHomeEvent.TabSelected -> state = state.copy(activeTab = event.tab)
            }
        },
    )
}
