package si.mentis.eprevzemmobile

import androidx.compose.runtime.Composable
import androidx.compose.ui.tooling.preview.Preview
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.feature.onboarding.WelcomeRoute

@Composable
@Preview
fun App() {
    EPrevzemTheme {
        WelcomeRoute(
            onRegisterDeviceClick = {
                // TODO: navigate to device-registration flow once it exists.
            },
        )
    }
}
