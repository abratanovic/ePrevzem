package si.mentis.eprevzemmobile.feature.registration.confirm

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier

@Composable
fun ConfirmAccountRoute(
    onBack: () -> Unit,
    onUseAnotherCode: () -> Unit,
    onConfirmed: () -> Unit = {},
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(ConfirmAccountState()) }

    ConfirmAccountScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                ConfirmAccountEvent.BackClicked -> onBack()
                ConfirmAccountEvent.UseAnotherCodeClicked -> onUseAnotherCode()
                ConfirmAccountEvent.SubmitClicked -> {
                    if (state.canSubmit) onConfirmed()
                }
                is ConfirmAccountEvent.BiometricToggled -> {
                    state = state.copy(isBiometricEnabled = event.enabled)
                }
                is ConfirmAccountEvent.PinChanged -> {
                    state = state.copy(pin = event.pin)
                }
                is ConfirmAccountEvent.PinConfirmationChanged -> {
                    state = state.copy(pinConfirmation = event.pin)
                }
                ConfirmAccountEvent.PinVisibilityToggled -> {
                    state = state.copy(isPinVisible = !state.isPinVisible)
                }
                ConfirmAccountEvent.PinConfirmationVisibilityToggled -> {
                    state = state.copy(
                        isPinConfirmationVisible = !state.isPinConfirmationVisible,
                    )
                }
            }
        },
    )
}
