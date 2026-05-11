package si.mentis.eprevzemmobile.feature.registration.confirm

sealed interface ConfirmAccountEvent {
    data object BackClicked : ConfirmAccountEvent
}
