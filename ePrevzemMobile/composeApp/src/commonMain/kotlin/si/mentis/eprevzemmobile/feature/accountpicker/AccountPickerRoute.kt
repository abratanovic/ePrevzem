package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.auth.SessionStore

@Composable
fun AccountPickerRoute(
    onAccountSelected: (String) -> Unit,
    onAddAccount: () -> Unit,
    sessionStore: SessionStore = AppContainer.sessionStore,
    modifier: Modifier = Modifier,
) {
    val profiles by sessionStore.profiles.collectAsState()
    val state = AccountPickerState(accounts = profiles.toAccountRows())

    AccountPickerScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                is AccountPickerEvent.AccountSelected -> onAccountSelected(event.accountId)
                AccountPickerEvent.AddAccountClicked -> onAddAccount()
            }
        },
    )
}
