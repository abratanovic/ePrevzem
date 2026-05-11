package si.mentis.eprevzemmobile.feature.registration.confirm

sealed interface ConfirmAccountEvent {
    data object BackClicked : ConfirmAccountEvent
    data object UseAnotherCodeClicked : ConfirmAccountEvent
    data object SubmitClicked : ConfirmAccountEvent
    data class BiometricToggled(val enabled: Boolean) : ConfirmAccountEvent
    data class PinChanged(val pin: String) : ConfirmAccountEvent
    data class PinConfirmationChanged(val pin: String) : ConfirmAccountEvent
    data object PinVisibilityToggled : ConfirmAccountEvent
    data object PinConfirmationVisibilityToggled : ConfirmAccountEvent
}
