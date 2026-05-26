package si.mentis.eprevzemmobile.feature.delegation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.delegation.DelegationRepository

private const val NotFoundTitle = "Oseba ni bila najdena"
private const val NotFoundMessage = "Preverite EMŠO ali se prepričajte, da je oseba registrirana v ePrevzem."
private const val DelegationErrorMessage = "Napaka pri pooblastitvi. Poskusite znova."

@Composable
fun DelegatePersonRoute(
    pickupId: String,
    onBack: () -> Unit,
    onDelegated: () -> Unit,
    repository: DelegationRepository = AppContainer.delegationRepository,
    securityRepository: si.mentis.eprevzemmobile.data.security.SecurityRepository = AppContainer.securityRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(DelegatePersonState()) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(state.isLoading) {
        if (!state.isLoading) return@LaunchedEffect
        repository.lookupByEmso(state.emso)
            .onSuccess { person ->
                state = state.copy(
                    isLoading = false,
                    foundPerson = person,
                    phase = DelegatePersonPhase.Preview,
                    errorTitle = null,
                    errorMessage = null,
                )
            }
            .onFailure {
                state = state.copy(
                    isLoading = false,
                    errorTitle = NotFoundTitle,
                    errorMessage = NotFoundMessage,
                )
            }
    }

    fun verifyBiometric() {
        scope.launch {
            securityRepository.signChallengeWithBiometric("verify".encodeToByteArray())
                .onSuccess {
                    state = state.copy(showBiometricSheet = false, isConfirming = true)
                }
                .onFailure {
                    state = state.copy(showBiometricSheet = false, showPinSheet = true)
                }
        }
    }

    fun verifyPin(pin: String) {
        scope.launch {
            securityRepository.signChallengeWithPin(pin, "verify".encodeToByteArray())
                .onSuccess {
                    state = state.copy(showPinSheet = false, pinValue = "", isConfirming = true)
                }
                .onFailure {
                    state = state.copy(pinValue = "")
                }
        }
    }

    LaunchedEffect(state.showBiometricSheet) {
        if (state.showBiometricSheet) {
            verifyBiometric()
        }
    }

    LaunchedEffect(state.isConfirming) {
        if (!state.isConfirming) return@LaunchedEffect
        repository.addDelegation(pickupId, state.emso)
            .onSuccess { onDelegated() }
            .onFailure {
                state = state.copy(
                    isConfirming = false,
                    phase = DelegatePersonPhase.Preview,
                    confirmError = DelegationErrorMessage,
                )
            }
    }

    DelegatePersonScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                is DelegatePersonEvent.EmsoChanged -> state = state.copy(
                    emso = event.emso,
                    errorTitle = null,
                    errorMessage = null,
                )
                DelegatePersonEvent.SearchClicked -> {
                    if (state.canSearch) {
                        state = state.copy(isLoading = true, errorTitle = null, errorMessage = null)
                    }
                }
                DelegatePersonEvent.PersonSelected -> state = state.copy(
                    phase = DelegatePersonPhase.Confirming,
                    showBiometricSheet = true,
                    confirmError = null,
                )
                DelegatePersonEvent.BiometricSelected -> state = state.copy(
                    showPinSheet = false,
                    showBiometricSheet = true,
                )
                DelegatePersonEvent.PinSelected -> state = state.copy(
                    showBiometricSheet = false,
                    showPinSheet = true,
                )
                is DelegatePersonEvent.PinDigitEntered -> {
                    val newPin = if (state.pinValue.length < 6) {
                        state.pinValue + event.digit.toString()
                    } else {
                        state.pinValue
                    }
                    state = state.copy(pinValue = newPin)
                    if (newPin.length == 6) {
                        verifyPin(newPin)
                    }
                }
                DelegatePersonEvent.PinBackspace -> {
                    if (state.pinValue.isNotEmpty()) {
                        state = state.copy(pinValue = state.pinValue.dropLast(1))
                    }
                }
                DelegatePersonEvent.ConfirmCancelled -> state = state.copy(
                    phase = DelegatePersonPhase.Preview,
                    showBiometricSheet = false,
                    showPinSheet = false,
                    pinValue = "",
                    isConfirming = false,
                )
                DelegatePersonEvent.Back -> when (state.phase) {
                    DelegatePersonPhase.Search -> onBack()
                    DelegatePersonPhase.Preview -> state = state.copy(
                        phase = DelegatePersonPhase.Search,
                        foundPerson = null,
                        confirmError = null,
                    )
                    DelegatePersonPhase.Confirming -> state = state.copy(
                        phase = DelegatePersonPhase.Preview,
                        showBiometricSheet = false,
                        showPinSheet = false,
                        pinValue = "",
                        isConfirming = false,
                    )
                }
            }
        },
    )
}
