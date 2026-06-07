package si.mentis.eprevzemmobile.feature.profile

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.auth.SessionStore
import si.mentis.eprevzemmobile.data.security.SecurityRepository
import si.mentis.eprevzemmobile.data.settings.UserSettingsRepository
import si.mentis.eprevzemmobile.domain.AppUser

/**
 * Stateful profile + settings tab shared by the citizen and operator flows. Owns the
 * biometric / PIN / notification state and the async settings handlers, and renders the
 * stateless [ProfileScreen]. The hosting flow only supplies the signed-in [user] and the
 * navigation callbacks.
 */
@Composable
fun ProfileContent(
    user: AppUser,
    onAddAccount: () -> Unit,
    onSwitchAccount: (String) -> Unit,
    onUserUpdated: (AppUser) -> Unit,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    userSettingsRepository: UserSettingsRepository = AppContainer.userSettingsRepository,
    sessionStore: SessionStore = AppContainer.sessionStore,
) {
    val scope = rememberCoroutineScope()
    val profiles by sessionStore.profiles.collectAsState()
    var state by remember(user.id) {
        mutableStateOf(
            ProfileUiState(
                userName = user.fullName,
                profile = user.toProfileData(),
                isBiometricEnabled = user.isBiometricEnabled,
            )
        )
    }

    LaunchedEffect(profiles, user.id) {
        state = state.copy(accounts = profiles.toProfileAccounts(activeId = user.id))
    }

    LaunchedEffect(user.id) {
        val biometricEnabled = securityRepository.isBiometricEnabled(user.id)
        val notificationsEnabled = userSettingsRepository.areNotificationsEnabled()
        state = state.copy(
            isBiometricEnabled = biometricEnabled,
            areNotificationsEnabled = notificationsEnabled,
        )
        val syncedUser = user.withBiometric(biometricEnabled)
        if (syncedUser != user) onUserUpdated(syncedUser)
    }

    ProfileScreen(
        state = state,
        onEvent = { event ->
            when (event) {
                is ProfileUiEvent.BiometricToggleRequested -> {
                    if (event.enabled) {
                        state = state.copy(
                            isBiometricPinSheetVisible = true,
                            biometricPin = "",
                            isBiometricPinVisible = false,
                            settingsError = null,
                        )
                    } else if (!state.isUpdatingSettings) {
                        state = state.copy(isUpdatingSettings = true, settingsError = null)
                        scope.launch {
                            securityRepository.disableBiometric(user.id)
                                .onSuccess {
                                    state = state.copy(
                                        isBiometricEnabled = false,
                                        isUpdatingSettings = false,
                                        settingsError = null,
                                    )
                                    onUserUpdated(user.withBiometric(false))
                                }
                                .onFailure {
                                    state = state.copy(
                                        isUpdatingSettings = false,
                                        settingsError = "Biometrije ni bilo mogoče izklopiti. Poskusite znova.",
                                    )
                                }
                        }
                    }
                }
                is ProfileUiEvent.BiometricPinChanged -> {
                    state = state.copy(
                        biometricPin = event.pin.take(ProfileUiState.BIOMETRIC_PIN_LENGTH),
                        settingsError = null,
                    )
                }
                ProfileUiEvent.BiometricPinVisibilityToggled -> {
                    state = state.copy(isBiometricPinVisible = !state.isBiometricPinVisible)
                }
                ProfileUiEvent.BiometricEnableConfirmed -> {
                    if (state.canConfirmBiometric && !state.isUpdatingSettings) {
                        val pin = state.biometricPin
                        state = state.copy(isUpdatingSettings = true, settingsError = null)
                        scope.launch {
                            securityRepository.enableBiometric(user.id, pin)
                                .onSuccess {
                                    state = state.copy(
                                        isBiometricEnabled = true,
                                        isBiometricPinSheetVisible = false,
                                        biometricPin = "",
                                        isBiometricPinVisible = false,
                                        isUpdatingSettings = false,
                                        settingsError = null,
                                    )
                                    onUserUpdated(user.withBiometric(true))
                                }
                                .onFailure {
                                    state = state.copy(
                                        isUpdatingSettings = false,
                                        settingsError = "PIN ni pravilen ali biometrična potrditev ni uspela.",
                                    )
                                }
                        }
                    }
                }
                ProfileUiEvent.BiometricEnableCancelled -> {
                    state = state.copy(
                        isBiometricPinSheetVisible = false,
                        biometricPin = "",
                        isBiometricPinVisible = false,
                        settingsError = null,
                    )
                }
                is ProfileUiEvent.NotificationsToggled -> {
                    state = state.copy(
                        areNotificationsEnabled = event.enabled,
                        settingsError = null,
                    )
                    scope.launch {
                        userSettingsRepository.setNotificationsEnabled(event.enabled)
                    }
                }
                ProfileUiEvent.ChangePinClicked -> {
                    state = state.copy(
                        isChangePinSheetVisible = true,
                        currentPin = "",
                        newPin = "",
                        newPinConfirmation = "",
                        isCurrentPinVisible = false,
                        isNewPinVisible = false,
                        isNewPinConfirmationVisible = false,
                        pinChangeError = null,
                    )
                }
                ProfileUiEvent.ChangePinCancelled -> {
                    state = state.copy(
                        isChangePinSheetVisible = false,
                        currentPin = "",
                        newPin = "",
                        newPinConfirmation = "",
                        isCurrentPinVisible = false,
                        isNewPinVisible = false,
                        isNewPinConfirmationVisible = false,
                        isChangingPin = false,
                        pinChangeError = null,
                    )
                }
                is ProfileUiEvent.CurrentPinChanged -> {
                    state = state.copy(
                        currentPin = event.pin.take(ProfileUiState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                is ProfileUiEvent.NewPinChanged -> {
                    state = state.copy(
                        newPin = event.pin.take(ProfileUiState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                is ProfileUiEvent.NewPinConfirmationChanged -> {
                    state = state.copy(
                        newPinConfirmation = event.pin.take(ProfileUiState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                ProfileUiEvent.CurrentPinVisibilityToggled -> {
                    state = state.copy(isCurrentPinVisible = !state.isCurrentPinVisible)
                }
                ProfileUiEvent.NewPinVisibilityToggled -> {
                    state = state.copy(isNewPinVisible = !state.isNewPinVisible)
                }
                ProfileUiEvent.NewPinConfirmationVisibilityToggled -> {
                    state = state.copy(isNewPinConfirmationVisible = !state.isNewPinConfirmationVisible)
                }
                ProfileUiEvent.ChangePinConfirmed -> {
                    if (state.canConfirmPinChange && !state.isChangingPin) {
                        val currentPin = state.currentPin
                        val newPin = state.newPin
                        state = state.copy(isChangingPin = true, pinChangeError = null)
                        scope.launch {
                            securityRepository.changePin(user.id, currentPin, newPin)
                                .onSuccess {
                                    state = state.copy(
                                        isChangePinSheetVisible = false,
                                        currentPin = "",
                                        newPin = "",
                                        newPinConfirmation = "",
                                        isCurrentPinVisible = false,
                                        isNewPinVisible = false,
                                        isNewPinConfirmationVisible = false,
                                        isChangingPin = false,
                                        pinChangeError = null,
                                    )
                                }
                                .onFailure {
                                    state = state.copy(
                                        isChangingPin = false,
                                        pinChangeError = "Trenutni PIN ni pravilen. Poskusite znova.",
                                    )
                                }
                        }
                    }
                }
                ProfileUiEvent.AvatarClicked -> {
                    state = state.copy(isAccountSwitcherVisible = true)
                }
                ProfileUiEvent.AccountSwitcherDismissed -> {
                    state = state.copy(isAccountSwitcherVisible = false)
                }
                is ProfileUiEvent.SwitchAccountRequested -> {
                    state = state.copy(isAccountSwitcherVisible = false)
                    onSwitchAccount(event.accountId)
                }
                ProfileUiEvent.AddAccountClicked -> {
                    state = state.copy(isAccountSwitcherVisible = false)
                    onAddAccount()
                }
            }
        },
    )
}

private fun List<AppUser>.toProfileAccounts(activeId: String): List<ProfileAccount> =
    map { user -> user.toProfileAccount(isActive = user.id == activeId) }
        .sortedByDescending { it.isActive }

private fun AppUser.toProfileAccount(isActive: Boolean): ProfileAccount = when (this) {
    is AppUser.Employee -> ProfileAccount(
        id = id,
        fullName = fullName,
        roleLabel = organizationName.takeIf { it.isNotBlank() }
            ?.let { "Zaposleni · $it" }
            ?: "Zaposleni",
        isActive = isActive,
    )
    is AppUser.RegularUser -> ProfileAccount(
        id = id,
        fullName = fullName,
        roleLabel = "Občan",
        isActive = isActive,
    )
}

private fun AppUser.toProfileData(): ProfileData = when (this) {
    is AppUser.Employee -> ProfileData(
        fullName = fullName,
        email = email,
        phone = phone,
        status = status,
        validUntil = validUntil,
        organizationName = organizationName,
        organizationType = organizationType,
        organizationLocation = organizationLocation,
    )
    is AppUser.RegularUser -> ProfileData(
        fullName = fullName,
        email = email,
        phone = phone,
    )
}

private fun AppUser.withBiometric(enabled: Boolean): AppUser = when (this) {
    is AppUser.Employee -> copy(isBiometricEnabled = enabled)
    is AppUser.RegularUser -> copy(isBiometricEnabled = enabled)
}
