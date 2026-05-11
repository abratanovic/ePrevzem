package si.mentis.eprevzemmobile.feature.registration.code

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import kotlinx.coroutines.delay

// Temporary stand-in for the real verifier. Compared hyphen-free, case-
// insensitive. Replace with a real network/repository call once one exists.
private const val VALID_CODE = "ABC123XYZ"

// Simulated round-trip while a real backend doesn't exist yet. Long enough
// to read as a deliberate verification step, short enough not to feel stuck.
private const val FAKE_VERIFY_DELAY_MS = 1200L

private const val InvalidCodeTitle = "Koda ni veljavna ali je potekla"
private const val InvalidCodeMessage =
    "Preverite vnos ali zahtevajte novo kodo pri svoji organizaciji."

/**
 * Stateful entry point for the device registration code screen. Owns the
 * transient UI state with `remember { mutableStateOf(...) }` until a
 * ViewModel layer is introduced. Caller wires up navigation through [onBack]
 * and is notified on successful verification through [onCodeAccepted].
 *
 * Verification is currently mocked against a hardcoded [VALID_CODE] with a
 * [FAKE_VERIFY_DELAY_MS] artificial delay to simulate a network round-trip.
 */
@Composable
fun RegistrationCodeRoute(
    onBack: () -> Unit,
    onCodeAccepted: (code: String) -> Unit = {},
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(RegistrationCodeState()) }

    // Drive the fake verification side-effect off isLoading. A real
    // implementation would replace this whole block with a repository call.
    LaunchedEffect(state.isLoading) {
        if (!state.isLoading) return@LaunchedEffect
        val candidate = state.code.replace("-", "")
        delay(FAKE_VERIFY_DELAY_MS)
        if (candidate.equals(VALID_CODE, ignoreCase = true)) {
            state = state.copy(isLoading = false)
            onCodeAccepted(candidate)
        } else {
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
                RegistrationCodeEvent.HelpClicked -> {
                    state = state.copy(isHelpVisible = true)
                }
                RegistrationCodeEvent.HelpDismissed -> {
                    state = state.copy(isHelpVisible = false)
                }
            }
        },
    )
}
