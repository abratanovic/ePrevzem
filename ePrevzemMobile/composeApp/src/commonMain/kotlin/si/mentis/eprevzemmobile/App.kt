package si.mentis.eprevzemmobile

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.tooling.preview.Preview
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.domain.User
import si.mentis.eprevzemmobile.feature.delegation.DelegatePersonRoute
import si.mentis.eprevzemmobile.feature.onboarding.WelcomeRoute
import si.mentis.eprevzemmobile.feature.pickups.ActivePickupsRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupConfirmedRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupDetailsRoute
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.registration.code.RegistrationCodeRoute
import si.mentis.eprevzemmobile.feature.registration.confirm.ConfirmAccountRoute
import si.mentis.eprevzemmobile.feature.unlock.UnlockRoute

private sealed interface AppDestination {
    data object Welcome : AppDestination
    data object RegistrationCode : AppDestination
    data class ConfirmAccount(val validatedCode: String) : AppDestination
    data object ActivePickups : AppDestination
    data class PickupDetails(val pickupId: String, val unlockedAt: String? = null) : AppDestination
    data class Unlock(val pickupId: String, val lockerNumber: String) : AppDestination
    data class DelegatePerson(val pickupId: String) : AppDestination
    data object PickupConfirmed : AppDestination
}

private val AppDestination.depth: Int get() = when (this) {
    AppDestination.Welcome -> 0
    AppDestination.RegistrationCode -> 1
    is AppDestination.ConfirmAccount -> 2
    AppDestination.ActivePickups -> 3
    is AppDestination.PickupDetails -> 4
    is AppDestination.Unlock -> 5
    is AppDestination.DelegatePerson -> 5
    AppDestination.PickupConfirmed -> 6
}

@Composable
@Preview
fun App() {
    EPrevzemTheme {
        var destination: AppDestination by remember { mutableStateOf(AppDestination.Welcome) }
        var currentUser: User? by remember { mutableStateOf(null) }
        var confirmedDetails: PickupDetails? by remember { mutableStateOf(null) }

        AnimatedContent(
            targetState = destination,
            transitionSpec = {
                val forward = targetState.depth >= initialState.depth
                val enter = slideInHorizontally(tween(280)) { if (forward) it / 5 else -it / 5 } +
                    fadeIn(tween(280))
                val exit = slideOutHorizontally(tween(280)) { if (forward) -it / 5 else it / 5 } +
                    fadeOut(tween(200))
                enter togetherWith exit
            },
            label = "screen_transition",
        ) { dest ->
            when (dest) {
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
                    initialUnlockedAt = dest.unlockedAt,
                    user = currentUser,
                    onBack = { destination = AppDestination.ActivePickups },
                    onIdentityVerified = { details ->
                        destination = AppDestination.Unlock(
                            pickupId = details.id,
                            lockerNumber = details.lockerNumber,
                        )
                    },
                    onLockerDidNotOpen = { details ->
                        destination = AppDestination.Unlock(
                            pickupId = details.id,
                            lockerNumber = details.lockerNumber,
                        )
                    },
                    onPickupConfirmed = { details ->
                        confirmedDetails = details
                        destination = AppDestination.PickupConfirmed
                    },
                    onDelegatePerson = { destination = AppDestination.DelegatePerson(dest.pickupId) },
                )
                is AppDestination.DelegatePerson -> DelegatePersonRoute(
                    pickupId = dest.pickupId,
                    onBack = { destination = AppDestination.PickupDetails(dest.pickupId) },
                    onDelegated = { destination = AppDestination.PickupDetails(dest.pickupId) },
                )
                is AppDestination.Unlock -> UnlockRoute(
                    pickupId = dest.pickupId,
                    expectedLockerNumber = dest.lockerNumber,
                    onBack = {
                        destination = AppDestination.PickupDetails(dest.pickupId)
                    },
                    onUnlocked = { unlockedAt ->
                        destination = AppDestination.PickupDetails(dest.pickupId, unlockedAt)
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
}
