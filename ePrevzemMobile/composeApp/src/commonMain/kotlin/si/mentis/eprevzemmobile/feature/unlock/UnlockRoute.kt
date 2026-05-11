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
import si.mentis.eprevzemmobile.core.camera.CameraPermissionStatus
import si.mentis.eprevzemmobile.core.camera.rememberCameraPermissionState
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.locker.OpenBoxResult

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
                phase = UnlockPhase.RequestingPermission,
            )
        )
    }
    val permission = rememberCameraPermissionState()
    val scope = rememberCoroutineScope()
    var lastScannedBoxId by remember { mutableStateOf<Long?>(null) }

    // Drive permission flow while the UI is in RequestingPermission.
    LaunchedEffect(permission.status, state.phase) {
        if (state.phase != UnlockPhase.RequestingPermission) return@LaunchedEffect
        when (permission.status) {
            CameraPermissionStatus.Granted -> {
                state = state.copy(phase = UnlockPhase.Scanning)
            }
            CameraPermissionStatus.Denied -> {
                state = state.copy(phase = UnlockPhase.ScanError(ScanErrorReason.CameraDenied))
            }
            CameraPermissionStatus.Unknown -> {
                permission.request()
            }
        }
    }

    fun startUnlock(boxId: Long) {
        lastScannedBoxId = boxId
        state = state.copy(phase = UnlockPhase.Unlocking)
        scope.launch {
            when (val result = lockerRepository.openBox(boxId)) {
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

    UnlockScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                UnlockEvent.Back -> {
                    if (state.phase != UnlockPhase.Unlocking) onBack()
                }
                UnlockEvent.RequestPermission -> permission.request()
                UnlockEvent.PermissionGranted -> state = state.copy(phase = UnlockPhase.Scanning)
                UnlockEvent.PermissionDenied ->
                    state = state.copy(phase = UnlockPhase.ScanError(ScanErrorReason.CameraDenied))
                UnlockEvent.OpenSettings -> permission.openAppSettings()
                is UnlockEvent.QrDetected -> {
                    if (state.phase !is UnlockPhase.Scanning) return@UnlockScreen
                    val scanned = event.raw.trim().toLongOrNull()
                    val expected = parseLocker(state.expectedLockerNumber)
                    state = when {
                        scanned == null ->
                            state.copy(phase = UnlockPhase.ScanError(ScanErrorReason.InvalidPayload))
                        expected != null && scanned != expected ->
                            state.copy(phase = UnlockPhase.ScanError(ScanErrorReason.WrongLocker))
                        else -> {
                            startUnlock(scanned)
                            state
                        }
                    }
                }
                UnlockEvent.DismissScanError -> {
                    state = state.copy(phase = UnlockPhase.Scanning)
                }
                UnlockEvent.Retry -> {
                    val box = lastScannedBoxId
                    if (box != null && state.attempt < UnlockState.MAX_ATTEMPTS) {
                        startUnlock(box)
                    }
                }
                UnlockEvent.ContactSupport -> onContactSupport()
            }
        },
    )
}

internal fun parseLocker(raw: String): Long? {
    val digits = raw.filter { it.isDigit() }
    return digits.toLongOrNull()
}

internal expect fun nowHhMm(): String
