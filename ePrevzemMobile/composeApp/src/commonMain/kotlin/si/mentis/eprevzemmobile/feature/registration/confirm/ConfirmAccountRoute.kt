package si.mentis.eprevzemmobile.feature.registration.confirm

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
import si.mentis.eprevzemmobile.data.registration.RegistrationRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository
import si.mentis.eprevzemmobile.domain.User

@Composable
fun ConfirmAccountRoute(
    validatedCode: String,
    onBack: () -> Unit,
    onUseAnotherCode: () -> Unit,
    onConfirmed: (User) -> Unit = {},
    repository: RegistrationRepository = AppContainer.registrationRepository,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(ConfirmAccountState()) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(validatedCode) {
        repository.fetchAccountPreview(validatedCode)
            .onSuccess { user ->
                state = state.copy(
                    account = ConfirmAccountData(
                        fullName = user.fullName,
                        email = user.email,
                        phone = user.phone,
                        status = user.status,
                        validUntil = user.validUntil,
                    ),
                    organization = ConfirmOrganizationData(
                        name = user.organizationName,
                        type = user.organizationType,
                        location = user.organizationLocation,
                    ),
                )
            }
    }

    ConfirmAccountScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                ConfirmAccountEvent.BackClicked -> onBack()
                ConfirmAccountEvent.UseAnotherCodeClicked -> onUseAnotherCode()
                ConfirmAccountEvent.SubmitClicked -> {
                    if (state.canSubmit && !state.isLoading) {
                        state = state.copy(isLoading = true, error = null)
                        scope.launch {
                            securityRepository.register(state.pin, state.isBiometricEnabled)
                                .onSuccess { publicKey ->
                                    repository.confirmAccount(validatedCode, publicKey)
                                        .onSuccess { user ->
                                            state = state.copy(isLoading = false)
                                            onConfirmed(user)
                                        }
                                        .onFailure {
                                            state = state.copy(
                                                isLoading = false,
                                                error = "Registracija ni uspela. Poskusite znova.",
                                            )
                                        }
                                }
                                .onFailure {
                                    state = state.copy(
                                        isLoading = false,
                                        error = "Varnostna nastavitev ni uspela: ${it::class.simpleName}: ${it.message}",
                                    )
                                }
                        }
                    }
                }
                is ConfirmAccountEvent.BiometricToggled -> {
                    state = state.copy(isBiometricEnabled = event.enabled)
                }
                is ConfirmAccountEvent.PinChanged -> {
                    state = state.copy(pin = event.pin, error = null)
                }
                is ConfirmAccountEvent.PinConfirmationChanged -> {
                    state = state.copy(pinConfirmation = event.pin, error = null)
                }
                ConfirmAccountEvent.PinVisibilityToggled -> {
                    state = state.copy(isPinVisible = !state.isPinVisible)
                }
                ConfirmAccountEvent.PinConfirmationVisibilityToggled -> {
                    state = state.copy(isPinConfirmationVisible = !state.isPinConfirmationVisible)
                }
            }
        },
    )
}
