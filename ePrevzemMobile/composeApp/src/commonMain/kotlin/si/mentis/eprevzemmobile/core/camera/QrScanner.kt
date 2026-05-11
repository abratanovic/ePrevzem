package si.mentis.eprevzemmobile.core.camera

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

enum class CameraPermissionStatus { Unknown, Granted, Denied }

interface CameraPermissionState {
    val status: CameraPermissionStatus
    fun request()
    fun openAppSettings()
}

@Composable
expect fun rememberCameraPermissionState(): CameraPermissionState

/**
 * Full-bleed camera preview that scans for QR codes and invokes [onQrDetected]
 * with the first decoded payload. The caller is responsible for navigating away
 * after a successful scan; this composable does not stop scanning on its own.
 */
@Composable
expect fun QrScannerView(
    modifier: Modifier = Modifier,
    onQrDetected: (String) -> Unit,
)
