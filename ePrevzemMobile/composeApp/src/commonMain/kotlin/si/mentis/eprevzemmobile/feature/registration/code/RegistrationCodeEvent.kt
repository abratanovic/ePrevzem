package si.mentis.eprevzemmobile.feature.registration.code

sealed interface RegistrationCodeEvent {
    data class CodeChanged(val code: String) : RegistrationCodeEvent
    data object SubmitClicked : RegistrationCodeEvent
    data object BackClicked : RegistrationCodeEvent
    data object HelpClicked : RegistrationCodeEvent
    data object HelpDismissed : RegistrationCodeEvent
}
