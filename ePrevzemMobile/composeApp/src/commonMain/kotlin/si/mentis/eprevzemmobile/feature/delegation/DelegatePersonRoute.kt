package si.mentis.eprevzemmobile.feature.delegation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.delegation.DelegationRepository

private const val NotFoundTitle = "Oseba ni bila najdena"
private const val NotFoundMessage = "Preverite EMŠO ali se prepričajte, da je oseba registrirana v ePrevzem."

@Composable
fun DelegatePersonRoute(
    pickupId: String,
    onBack: () -> Unit,
    onDelegated: () -> Unit,
    repository: DelegationRepository = AppContainer.delegationRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(DelegatePersonState()) }

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
                )
                DelegatePersonEvent.Back -> when (state.phase) {
                    DelegatePersonPhase.Search -> onBack()
                    DelegatePersonPhase.Preview -> state = state.copy(
                        phase = DelegatePersonPhase.Search,
                        foundPerson = null,
                    )
                    DelegatePersonPhase.Confirming -> state = state.copy(
                        phase = DelegatePersonPhase.Preview,
                    )
                }
                // Confirmation events wired in PRVZM-30
                DelegatePersonEvent.BiometricSelected,
                DelegatePersonEvent.PinSelected,
                is DelegatePersonEvent.PinDigitEntered,
                DelegatePersonEvent.PinBackspace,
                DelegatePersonEvent.ConfirmCancelled -> {}
            }
        },
    )
}
