package si.mentis.eprevzemmobile.feature.registration.code

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.registration.RegistrationRepository

private const val InvalidCodeTitle = "Koda ni veljavna ali je potekla"
private const val InvalidCodeMessage =
    "Preverite vnos ali zahtevajte novo kodo pri svoji organizaciji."

@Composable
fun RegistrationCodeRoute(
    onBack: () -> Unit,
    onCodeAccepted: (code: String) -> Unit = {},
    repository: RegistrationRepository = AppContainer.registrationRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(RegistrationCodeState()) }

    LaunchedEffect(state.isLoading) {
        if (!state.isLoading) return@LaunchedEffect
        repository.validateCode(state.code)
            .onSuccess { code ->
                state = state.copy(isLoading = false)
                onCodeAccepted(code)
            }
            .onFailure {
                state = state.copy(
                    isLoading = false,
                    errorTitle = InvalidCodeTitle,
                    errorMessage = InvalidCodeMessage,
                )
            }
    }

    RegistrationCodeScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                is RegistrationCodeEvent.CodeChanged -> {
                    state = state.copy(
                        code = event.code,
                        errorTitle = null,
                        errorMessage = null,
                    )
                }
                RegistrationCodeEvent.SubmitClicked -> {
                    if (state.canSubmit) {
                        state = state.copy(
                            isLoading = true,
                            errorTitle = null,
                            errorMessage = null,
                        )
                    }
                }
                RegistrationCodeEvent.BackClicked -> onBack()
                RegistrationCodeEvent.HelpClicked -> state = state.copy(isHelpVisible = true)
                RegistrationCodeEvent.HelpDismissed -> state = state.copy(isHelpVisible = false)
            }
        },
    )
}
