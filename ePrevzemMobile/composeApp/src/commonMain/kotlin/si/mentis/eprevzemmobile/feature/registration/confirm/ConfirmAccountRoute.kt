package si.mentis.eprevzemmobile.feature.registration.confirm

import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier

@Composable
fun ConfirmAccountRoute(
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val state = remember { ConfirmAccountState() }

    ConfirmAccountScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                ConfirmAccountEvent.BackClicked -> onBack()
            }
        },
    )
}
