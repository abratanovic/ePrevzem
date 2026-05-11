package si.mentis.eprevzemmobile.core.camera

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color

@Composable
actual fun rememberCameraPermissionState(): CameraPermissionState =
    remember {
        object : CameraPermissionState {
            override val status: CameraPermissionStatus = CameraPermissionStatus.Denied
            override fun request() {}
            override fun openAppSettings() {}
        }
    }

@Composable
actual fun QrScannerView(
    modifier: Modifier,
    onQrDetected: (String) -> Unit,
) {
    Box(modifier = modifier.fillMaxSize().background(Color.Black), contentAlignment = Alignment.Center) {
        Text("QR scanning not yet implemented on iOS", color = Color.White)
    }
}
