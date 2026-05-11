package si.mentis.eprevzemmobile.feature.onboarding

sealed interface WelcomeEvent {
    data object RegisterDeviceClicked : WelcomeEvent
    data object HelpClicked : WelcomeEvent
    data object HelpDismissed : WelcomeEvent
}
