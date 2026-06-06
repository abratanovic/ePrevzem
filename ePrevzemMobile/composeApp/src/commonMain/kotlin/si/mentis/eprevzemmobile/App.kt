package si.mentis.eprevzemmobile

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.tooling.preview.Preview
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import kotlin.time.TimeSource
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import androidx.compose.runtime.collectAsState
import si.mentis.eprevzemmobile.data.auth.AuthSession
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.feature.delegation.DelegatePersonRoute
import si.mentis.eprevzemmobile.feature.login.LoginRoute
import si.mentis.eprevzemmobile.feature.onboarding.WelcomeRoute
import si.mentis.eprevzemmobile.feature.operator.OperatorHomeRoute
import si.mentis.eprevzemmobile.feature.pickups.ActivePickupsRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupConfirmedRoute
import si.mentis.eprevzemmobile.feature.pickups.PickupDetailsRoute
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.registration.code.RegistrationCodeRoute
import si.mentis.eprevzemmobile.feature.registration.confirm.ConfirmAccountRoute
import si.mentis.eprevzemmobile.feature.unlock.UnlockRoute

private sealed interface AppDestination {
    data object Loading : AppDestination
    data object Welcome : AppDestination
    data object Login : AppDestination
    data object RegistrationCode : AppDestination
    data class ConfirmAccount(val validatedCode: String) : AppDestination
    data object ActivePickups : AppDestination
    data object OperatorHome : AppDestination
    data class PickupDetails(val pickupId: String, val unlockedAt: String? = null) : AppDestination
    data class Unlock(val pickupId: String, val lockerNumber: String) : AppDestination
    data class DelegatePerson(val pickupId: String) : AppDestination
    data object PickupConfirmed : AppDestination
}

private val AppDestination.depth: Int get() = when (this) {
    AppDestination.Loading -> -1
    AppDestination.Welcome -> 0
    AppDestination.Login -> 0
    AppDestination.RegistrationCode -> 1
    is AppDestination.ConfirmAccount -> 2
    AppDestination.ActivePickups -> 3
    AppDestination.OperatorHome -> 3
    is AppDestination.PickupDetails -> 4
    is AppDestination.Unlock -> 5
    is AppDestination.DelegatePerson -> 5
    AppDestination.PickupConfirmed -> 6
}

private const val BACKGROUND_LOCK_SECONDS = 120L

@Composable
@Preview
fun App() {
    EPrevzemTheme {
        val session by AppContainer.sessionStore.session.collectAsState()
        var destination: AppDestination by remember { mutableStateOf(AppDestination.Loading) }
        var confirmedDetails: PickupDetails? by remember { mutableStateOf(null) }
        val scope = rememberCoroutineScope()
        val lifecycleOwner = LocalLifecycleOwner.current
        var backgroundedAt by remember { mutableStateOf<TimeSource.Monotonic.ValueTimeMark?>(null) }

        LaunchedEffect(Unit) {
            AppContainer.sessionStore.hydrate()
        }

        LaunchedEffect(session) {
            destination = when (session) {
                AuthSession.Unknown -> AppDestination.Loading
                AuthSession.Unauthenticated -> if (AppContainer.securityRepository.isRegistered()) {
                    AppDestination.Login
                } else {
                    AppDestination.Welcome
                }
                is AuthSession.Authenticated -> {
                    val authedSession = session as AuthSession.Authenticated
                    val target = when (authedSession.user) {
                        is AppUser.RegularUser -> AppDestination.ActivePickups
                        is AppUser.Employee    -> AppDestination.OperatorHome
                    }
                    when (destination) {
                        AppDestination.Loading,
                        AppDestination.Welcome,
                        AppDestination.Login,
                        AppDestination.RegistrationCode,
                        is AppDestination.ConfirmAccount -> target
                        else -> destination
                    }
                }
            }
        }

        DisposableEffect(lifecycleOwner) {
            val observer = LifecycleEventObserver { _, event ->
                when (event) {
                    Lifecycle.Event.ON_PAUSE -> backgroundedAt = TimeSource.Monotonic.markNow()
                    Lifecycle.Event.ON_RESUME -> {
                        val since = backgroundedAt
                        backgroundedAt = null
                        if (since != null && since.elapsedNow().inWholeSeconds > BACKGROUND_LOCK_SECONDS) {
                            if (session is AuthSession.Authenticated) {
                                scope.launch { AppContainer.sessionStore.clear() }
                            }
                        }
                    }
                    else -> {}
                }
            }
            lifecycleOwner.lifecycle.addObserver(observer)
            onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
        }

        AnimatedContent(
            targetState = destination,
            transitionSpec = {
                if (initialState is AppDestination.Loading) {
                    fadeIn(tween(200)) togetherWith fadeOut(tween(200))
                } else {
                    val forward = targetState.depth >= initialState.depth
                    val enter = slideInHorizontally(tween(280)) { if (forward) it / 5 else -it / 5 } +
                        fadeIn(tween(280))
                    val exit = slideOutHorizontally(tween(280)) { if (forward) -it / 5 else it / 5 } +
                        fadeOut(tween(200))
                    enter togetherWith exit
                }
            },
            label = "screen_transition",
        ) { dest ->
            when (dest) {
                AppDestination.Loading -> Unit
                AppDestination.Login -> LoginRoute(
                    onResetSecureStorage = {
                        destination = AppDestination.Welcome
                    },
                )
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
                )
                AppDestination.ActivePickups -> {
                    val user = (session as? AuthSession.Authenticated)?.user
                    if (user != null) {
                        ActivePickupsRoute(
                            user = user,
                            onPickupClicked = { id -> destination = AppDestination.PickupDetails(id) },
                            onUserUpdated = { updatedUser ->
                                scope.launch { AppContainer.sessionStore.addProfile(updatedUser) }
                            },
                        )
                    }
                }
                AppDestination.OperatorHome -> {
                    val authenticated = session as? AuthSession.Authenticated
                    val employee = authenticated?.user as? AppUser.Employee
                    if (employee != null) {
                        OperatorHomeRoute(
                            user = employee,
                            onScanQrClicked = {
                                // TODO(operator-paketnik-plan): navigate to OperatorScan
                            },
                        )
                    }
                }
                is AppDestination.PickupDetails -> PickupDetailsRoute(
                    pickupId = dest.pickupId,
                    initialUnlockedAt = dest.unlockedAt,
                    user = (session as? AuthSession.Authenticated)?.user,
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
