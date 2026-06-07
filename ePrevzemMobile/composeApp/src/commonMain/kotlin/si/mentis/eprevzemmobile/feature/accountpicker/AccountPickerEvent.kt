package si.mentis.eprevzemmobile.feature.accountpicker

sealed interface AccountPickerEvent {
    data class AccountSelected(val accountId: String) : AccountPickerEvent
    data object AddAccountClicked : AccountPickerEvent
}
