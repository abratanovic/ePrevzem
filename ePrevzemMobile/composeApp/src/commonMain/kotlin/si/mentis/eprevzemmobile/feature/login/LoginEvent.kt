package si.mentis.eprevzemmobile.feature.login

sealed interface LoginEvent {
    data object BiometricRequested : LoginEvent
    data object SwitchToPinClicked : LoginEvent
    data class PinDigitEntered(val digit: Int) : LoginEvent
    data object PinBackspaceClicked : LoginEvent
    data object ResetSecureStorageClicked : LoginEvent
}
