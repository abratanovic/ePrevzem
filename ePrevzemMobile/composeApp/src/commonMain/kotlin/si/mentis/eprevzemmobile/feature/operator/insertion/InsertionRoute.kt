package si.mentis.eprevzemmobile.feature.operator.insertion

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.foundation.layout.fillMaxSize
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.core.audio.TokenAudioPlayer
import si.mentis.eprevzemmobile.core.camera.CameraPermissionStatus
import si.mentis.eprevzemmobile.core.camera.QrScannerView
import si.mentis.eprevzemmobile.core.camera.rememberCameraPermissionState
import si.mentis.eprevzemmobile.data.insertion.InsertionRepository
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.locker.OpenBoxResult

/**
 * Stateful entry for the operator insertion flow: scan station → pick package +
 * free locker → open (plays the token) → confirm closed → done. Owns camera
 * permission and supplies the scanner slot to the stateless [InsertionScreen].
 */
@Composable
fun InsertionRoute(
    onBack: () -> Unit,
    onDone: () -> Unit,
    insertionRepository: InsertionRepository = AppContainer.insertionRepository,
    lockerRepository: LockerRepository = AppContainer.lockerRepository,
    audioPlayer: TokenAudioPlayer = remember { TokenAudioPlayer() },
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(InsertionState()) }
    val scope = rememberCoroutineScope()
    val permission = rememberCameraPermissionState()

    LaunchedEffect(permission.status, state.step) {
        if (state.step != InsertionStep.Scan) return@LaunchedEffect
        when (permission.status) {
            CameraPermissionStatus.Unknown -> permission.request()
            CameraPermissionStatus.Denied ->
                state = state.copy(error = "Za skeniranje potrebujemo dostop do kamere.")
            CameraPermissionStatus.Granted -> state = state.copy(error = null)
        }
    }

    fun loadContext(serial: String) {
        state = state.copy(step = InsertionStep.Loading, error = null)
        scope.launch {
            insertionRepository.getContext(serial.trim())
                .onSuccess { ctx -> state = state.copy(step = InsertionStep.Select, context = ctx) }
                .onFailure {
                    state = state.copy(
                        step = InsertionStep.Scan,
                        error = "Paketnik ni bil najden ali ni dodeljen vaši organizaciji.",
                    )
                }
        }
    }

    fun openLocker() {
        val packageId = state.selectedPackageId ?: return
        val lockerId = state.selectedLockerId ?: return
        state = state.copy(step = InsertionStep.Opening, error = null)
        scope.launch {
            when (val result = lockerRepository.openForInsertion(packageId, lockerId)) {
                is OpenBoxResult.Success -> {
                    try {
                        audioPlayer.play(result.tokenWavBytes)
                    } catch (_: Throwable) {
                        // Playback hiccup: still let the operator try the physical insert.
                    }
                    state = state.copy(step = InsertionStep.Opened)
                }
                is OpenBoxResult.ApiFailure ->
                    state = state.copy(step = InsertionStep.Select, error = "Predalčka ni bilo mogoče odpreti. Poskusite znova.")
                is OpenBoxResult.NetworkFailure ->
                    state = state.copy(step = InsertionStep.Select, error = "Napaka pri povezavi. Poskusite znova.")
            }
        }
    }

    fun confirmInsertion() {
        val packageId = state.selectedPackageId ?: return
        val lockerId = state.selectedLockerId ?: return
        state = state.copy(step = InsertionStep.Confirming, error = null)
        scope.launch {
            insertionRepository.confirmInsertion(packageId, lockerId)
                .onSuccess { state = state.copy(step = InsertionStep.Done) }
                .onFailure {
                    state = state.copy(
                        step = InsertionStep.Opened,
                        error = "Vstavljanja ni bilo mogoče shraniti. Poskusite znova.",
                    )
                }
        }
    }

    fun handleEvent(event: InsertionEvent) {
        when (event) {
            is InsertionEvent.StationScanned -> if (state.step == InsertionStep.Scan) loadContext(event.serial)
            is InsertionEvent.PackageSelected -> state = state.copy(selectedPackageId = event.id, error = null)
            is InsertionEvent.LockerSelected -> state = state.copy(selectedLockerId = event.id, error = null)
            InsertionEvent.OpenClicked -> openLocker()
            InsertionEvent.ConfirmClosedClicked -> confirmInsertion()
            InsertionEvent.Retry -> openLocker()
            InsertionEvent.Back -> onBack()
            InsertionEvent.Done -> onDone()
        }
    }

    val scanner: @Composable () -> Unit = {
        if (permission.status == CameraPermissionStatus.Granted) {
            QrScannerView(
                modifier = Modifier.fillMaxSize(),
                onQrDetected = { raw -> handleEvent(InsertionEvent.StationScanned(raw)) },
            )
        }
    }

    InsertionScreen(
        state = state,
        onEvent = ::handleEvent,
        scanner = scanner,
        modifier = modifier,
    )
}
