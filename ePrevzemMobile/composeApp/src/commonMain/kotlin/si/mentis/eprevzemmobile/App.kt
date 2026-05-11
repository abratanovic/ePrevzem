package si.mentis.eprevzemmobile

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.tooling.preview.Preview
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.domain.User
import si.mentis.eprevzemmobile.feature.onboarding.WelcomeRoute
import si.mentis.eprevzemmobile.feature.pickups.ActivePickupsRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupConfirmedRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupDetailsRoute
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.registration.code.RegistrationCodeRoute
import si.mentis.eprevzemmobile.feature.registration.confirm.ConfirmAccountRoute

private sealed interface AppDestination {
    data object Welcome : AppDestination
    data object RegistrationCode : AppDestination
    data class ConfirmAccount(val validatedCode: String) : AppDestination
    data object ActivePickups : AppDestination
    data class PickupDetails(val pickupId: String) : AppDestination
    data object PickupConfirmed : AppDestination
}

@Composable
@Preview
fun App() {
    EPrevzemTheme {
        var destination: AppDestination by remember { mutableStateOf(AppDestination.Welcome) }
        var currentUser: User? by remember { mutableStateOf(null) }
        var confirmedDetails: PickupDetails? by remember { mutableStateOf(null) }

        when (val dest = destination) {
            AppDestination.Welcome -> WelcomeRoute(
                onRegisterDeviceClick = { destination = AppDestination.RegistrationCode },
            )
            AppDestination.RegistrationCode -> RegistrationCodeRoute(
                onBack = { destination = AppDestination.Welcome },
                onCodeAccepted = { code -> destination = AppDestination.ConfirmAccount(code) },
            )
            is AppDestination.ConfirmAccount -> ConfirmAccountRoute(
                validatedCode = dest.validatedCode,
                onBack = { destination = AppDestination.RegistrationCode },
                onUseAnotherCode = { destination = AppDestination.RegistrationCode },
                onConfirmed = { user ->
                    currentUser = user
                    destination = AppDestination.ActivePickups
                },
            )
            AppDestination.ActivePickups -> {
                val user = currentUser
                if (user != null) {
                    ActivePickupsRoute(
                        user = user,
                        onPickupClicked = { id -> destination = AppDestination.PickupDetails(id) },
                    )
                }
            }
            is AppDestination.PickupDetails -> PickupDetailsRoute(
                pickupId = dest.pickupId,
                user = currentUser,
                onBack = { destination = AppDestination.ActivePickups },
                onPickupConfirmed = { details ->
                    confirmedDetails = details
                    destination = AppDestination.PickupConfirmed
                },
            )
            AppDestination.PickupConfirmed -> {
                val details = confirmedDetails
                if (details != null) {
                    PickupConfirmedRoute(
                        details = details,
                        onFinish = { destination = AppDestination.ActivePickups },
                    )
                }
            }
        }
    }
}
