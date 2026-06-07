package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.logevent.LogEventRepository
import si.mentis.eprevzemmobile.domain.AppUser

/**
 * Stateful history tab shared by the citizen and operator flows. Loads the audit log for the
 * signed-in [user] — the repository routes to the citizen or operator endpoint based on the
 * active profile — and renders the stateless [AuditLogScreen] with pull-to-refresh.
 */
@Composable
fun HistoryContent(
    user: AppUser,
    logEventRepository: LogEventRepository = AppContainer.logEventRepository,
) {
    val scope = rememberCoroutineScope()
    var entries by remember(user.id) { mutableStateOf<List<AuditLogEntry>>(emptyList()) }
    var isRefreshing by remember(user.id) { mutableStateOf(false) }

    val load: suspend () -> Unit = {
        isRefreshing = true
        entries = logEventRepository.getLogEventsForCurrentUser().map { it.toAuditLogEntry() }
        isRefreshing = false
    }

    LaunchedEffect(user.id) { load() }

    AuditLogScreen(
        entries = entries,
        isRefreshing = isRefreshing,
        onRefresh = { scope.launch { load() } },
    )
}
