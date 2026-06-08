package si.mentis.eprevzemmobile.feature.unlock

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.core.audio.TokenAudioPlayer
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.locker.OpenBoxResult

/**
 * Citizen locker-open flow. The backend resolves the box from the pickup and
 * returns the audio token — there is no QR scan. On entry the route opens the
 * locker, plays the token, and reports the unlock time; failures offer a retry.
 */
@Composable
fun UnlockRoute(
    pickupId: String,
    expectedLockerNumber: String,
    onBack: () -> Unit,
    onUnlocked: (unlockedAt: String) -> Unit,
    onContactSupport: () -> Unit = onBack,
    lockerRepository: LockerRepository = AppContainer.lockerRepository,
    audioPlayer: TokenAudioPlayer = remember { TokenAudioPlayer() },
    modifier: Modifier = Modifier,
) {
    var state by remember(pickupId) {
        mutableStateOf(
            UnlockState(
                pickupId = pickupId,
                expectedLockerNumber = expectedLockerNumber,
                phase = UnlockPhase.Unlocking,
            )
        )
    }
    val scope = rememberCoroutineScope()

    fun performOpen() {
        state = state.copy(phase = UnlockPhase.Unlocking)
        scope.launch {
            when (val result = lockerRepository.openForPickup(pickupId)) {
                is OpenBoxResult.Success -> {
                    try {
                        audioPlayer.play(result.tokenWavBytes)
                        state = state.copy(phase = UnlockPhase.Unlocked)
                        onUnlocked(nowHhMm())
                    } catch (_: Throwable) {
                        state = state.copy(
                            phase = UnlockPhase.Failed(UnlockError.PlaybackFailed),
                            attempt = state.attempt + 1,
                        )
                    }
                }
                is OpenBoxResult.ApiFailure -> {
                    state = state.copy(
                        phase = UnlockPhase.Failed(UnlockError.Api(result.errorNumber)),
                        attempt = state.attempt + 1,
                    )
                }
                is OpenBoxResult.NetworkFailure -> {
                    state = state.copy(
                        phase = UnlockPhase.Failed(UnlockError.Network),
                        attempt = state.attempt + 1,
                    )
                }
            }
        }
    }

    LaunchedEffect(pickupId) {
        performOpen()
    }

    UnlockScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                UnlockEvent.Back -> {
                    if (state.phase != UnlockPhase.Unlocking) onBack()
                }
                UnlockEvent.Retry -> {
                    if (state.attempt < UnlockState.MAX_ATTEMPTS) performOpen()
                }
                UnlockEvent.ContactSupport -> onContactSupport()
                // Scan-related events are unused in the backend-driven flow.
                UnlockEvent.RequestPermission,
                UnlockEvent.PermissionGranted,
                UnlockEvent.PermissionDenied,
                UnlockEvent.OpenSettings,
                UnlockEvent.DismissScanError,
                is UnlockEvent.QrDetected -> Unit
            }
        },
    )
}

internal expect fun nowHhMm(): String
