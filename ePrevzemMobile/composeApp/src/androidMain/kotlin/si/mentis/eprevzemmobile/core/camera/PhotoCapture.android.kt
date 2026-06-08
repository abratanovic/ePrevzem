package si.mentis.eprevzemmobile.core.camera

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.FileProvider
import java.io.File

@Composable
actual fun rememberPhotoCaptureLauncher(onCapture: (ByteArray?) -> Unit): PhotoCaptureLauncher {
    val context = LocalContext.current
    val latestCapture by rememberUpdatedState(onCapture)
    val fileHolder = remember { FileHolder() }

    val launcher = rememberLauncherForActivityResult(ActivityResultContracts.TakePicture()) { success ->
        val file = fileHolder.file
        if (success && file != null && file.exists() && file.length() > 0) {
            latestCapture(file.readBytes())
        } else {
            latestCapture(null)
        }
    }

    return remember(launcher) {
        object : PhotoCaptureLauncher {
            override fun launch() {
                val file = File(context.cacheDir, "capture_${System.nanoTime()}.jpg")
                fileHolder.file = file
                val uri = FileProvider.getUriForFile(
                    context,
                    "${context.packageName}.fileprovider",
                    file,
                )
                launcher.launch(uri)
            }
        }
    }
}

private class FileHolder(var file: File? = null)
