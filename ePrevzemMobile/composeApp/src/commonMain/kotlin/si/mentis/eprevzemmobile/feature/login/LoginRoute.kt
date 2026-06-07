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
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore
import si.mentis.eprevzemmobile.data.auth.SessionStore
import si.mentis.eprevzemmobile.data.security.AuthRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository

@Composable
fun LoginRoute(
    accountId: String,
    onResetSecureStorage: () -> Unit,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    authRepository: AuthRepository = AppContainer.authRepository,
    sessionStore: SessionStore = AppContainer.sessionStore,
    deviceSessionStore: DeviceSessionStore = AppContainer.deviceSessionStore,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(LoginState()) }
    val scope = rememberCoroutineScope()

    suspend fun finishAuthenticated() {
        sessionStore.setAuthenticated(accountId)
    }

    fun resetThisAccount() {
        scope.launch {
            securityRepository.reset(accountId)
            sessionStore.removeProfile(accountId)
            onResetSecureStorage()
        }
    }

    fun authWithBiometric() {
        scope.launch {
            state = state.copy(isLoading = true, error = null)
            val deviceId = deviceSessionStore.deviceId(accountId)
            if (deviceId == null) {
                resetThisAccount()
                return@launch
            }
            val challenge = authRepository.getChallenge(deviceId).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithBiometric(accountId, challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(deviceId, signature)
                        .onSuccess { finishAuthenticated() }
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
            val deviceId = deviceSessionStore.deviceId(accountId)
            if (deviceId == null) {
                resetThisAccount()
                return@launch
            }
            val challenge = authRepository.getChallenge(deviceId).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithPin(accountId, pin, challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(deviceId, signature)
                        .onSuccess { finishAuthenticated() }
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
                LoginEvent.ResetSecureStorageClicked -> resetThisAccount()
            }
        },
    )
}
