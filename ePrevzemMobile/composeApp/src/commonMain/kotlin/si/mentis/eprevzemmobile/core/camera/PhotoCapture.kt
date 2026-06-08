package si.mentis.eprevzemmobile.core.camera

import androidx.compose.runtime.Composable

interface PhotoCaptureLauncher {
    fun launch()
}

@Composable
expect fun rememberPhotoCaptureLauncher(onCapture: (ByteArray?) -> Unit): PhotoCaptureLauncher
