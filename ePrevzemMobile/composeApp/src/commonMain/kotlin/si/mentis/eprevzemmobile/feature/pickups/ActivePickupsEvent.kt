package si.mentis.eprevzemmobile.feature.pickups

sealed interface ActivePickupsEvent {
    data object Refresh : ActivePickupsEvent
    data class PickupClicked(val id: String) : ActivePickupsEvent
    data class TabSelected(val tab: ActiveTab) : ActivePickupsEvent
    data class BiometricToggleRequested(val enabled: Boolean) : ActivePickupsEvent
    data class BiometricPinChanged(val pin: String) : ActivePickupsEvent
    data object BiometricPinVisibilityToggled : ActivePickupsEvent
    data object BiometricEnableConfirmed : ActivePickupsEvent
    data object BiometricEnableCancelled : ActivePickupsEvent
    data class NotificationsToggled(val enabled: Boolean) : ActivePickupsEvent
}
