package si.mentis.eprevzemmobile

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.tooling.preview.Preview
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.feature.onboarding.WelcomeRoute
import si.mentis.eprevzemmobile.feature.registration.code.RegistrationCodeRoute

private sealed interface AppDestination {
    data object Welcome : AppDestination
    data object RegistrationCode : AppDestination
}

@Composable
@Preview
fun App() {
    EPrevzemTheme {
        var destination: AppDestination by remember { mutableStateOf(AppDestination.Welcome) }

        when (destination) {
            AppDestination.Welcome -> WelcomeRoute(
                onRegisterDeviceClick = { destination = AppDestination.RegistrationCode },
            )
            AppDestination.RegistrationCode -> RegistrationCodeRoute(
                onBack = { destination = AppDestination.Welcome },
                onCodeAccepted = {
                    // TODO: navigate forward once the post-registration flow exists.
                },
            )
        }
    }
}
