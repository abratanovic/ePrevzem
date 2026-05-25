package si.mentis.eprevzemmobile.feature.delegation

sealed interface DelegatePersonEvent {
    data class EmsoChanged(val emso: String) : DelegatePersonEvent
    data object SearchClicked : DelegatePersonEvent
    data object PersonSelected : DelegatePersonEvent
    data object BiometricSelected : DelegatePersonEvent
    data object PinSelected : DelegatePersonEvent
    data class PinDigitEntered(val digit: Int) : DelegatePersonEvent
    data object PinBackspace : DelegatePersonEvent
    data object ConfirmCancelled : DelegatePersonEvent
    data object Back : DelegatePersonEvent
}
