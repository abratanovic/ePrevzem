package si.mentis.eprevzemmobile.feature.login

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.security.AuthRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository
import si.mentis.eprevzemmobile.domain.User

private const val DEVICE_ID = "device-01"

@Composable
fun LoginRoute(
    onAuthenticated: (User) -> Unit,
    onResetSecureStorage: () -> Unit,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    authRepository: AuthRepository = AppContainer.authRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(LoginState()) }
    val scope = rememberCoroutineScope()

    fun authWithBiometric() {
        scope.launch {
            state = state.copy(isLoading = true, error = null)
            val challenge = authRepository.getChallenge(DEVICE_ID).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithBiometric(challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(DEVICE_ID, signature)
                        .onSuccess { onAuthenticated(cachedUser()) }
                        .onFailure {
                            state = state.copy(isLoading = false, error = "Avtentikacija ni uspela.")
                        }
                }
                .onFailure {
                    state = state.copy(isLoading = false, phase = LoginPhase.Pin)
                }
        }
    }

    fun authWithPin(pin: String) {
        scope.launch {
            state = state.copy(isLoading = true, error = null)
            val challenge = authRepository.getChallenge(DEVICE_ID).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithPin(pin, challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(DEVICE_ID, signature)
                        .onSuccess { onAuthenticated(cachedUser()) }
                        .onFailure {
                            state = state.copy(isLoading = false, error = "Avtentikacija ni uspela.")
                        }
                }
                .onFailure {
                    state = state.copy(isLoading = false, pin = "", error = "Napačen PIN. Poskusite znova.")
                }
        }
    }

    LaunchedEffect(Unit) {
        authWithBiometric()
    }

    LoginScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                LoginEvent.BiometricRequested -> if (!state.isLoading) authWithBiometric()
                LoginEvent.SwitchToPinClicked -> {
                    state = state.copy(phase = LoginPhase.Pin, error = null, isLoading = false)
                }
                is LoginEvent.PinDigitEntered -> {
                    if (state.pin.length < LoginState.PIN_LENGTH && !state.isLoading) {
                        val newPin = state.pin + event.digit
                        state = state.copy(pin = newPin, error = null)
                        if (newPin.length == LoginState.PIN_LENGTH) {
                            authWithPin(newPin)
                        }
                    }
                }
                LoginEvent.PinBackspaceClicked -> {
                    if (!state.isLoading) {
                        state = state.copy(pin = state.pin.dropLast(1), error = null)
                    }
                }
                LoginEvent.ResetSecureStorageClicked -> {
                    scope.launch {
                        securityRepository.reset()
                        onResetSecureStorage()
                    }
                }
            }
        },
    )
}

private fun cachedUser() = User(
    id = DEVICE_ID,
    fullName = "Marko Horvat",
    email = "marko.horvat@gov.si",
    phone = "+386 41 234 567",
    status = "Aktiven",
    validUntil = "14. nov 2025",
    organizationName = "Upravna enota Ljubljana",
    organizationType = "Javna uprava",
    organizationLocation = "Adamič-Lundrovo nabrežje 2, Ljubljana",
)
