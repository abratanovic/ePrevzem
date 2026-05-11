package si.mentis.eprevzemmobile.core.audio

import android.media.AudioAttributes
import android.media.MediaPlayer
import kotlinx.coroutines.suspendCancellableCoroutine
import si.mentis.eprevzemmobile.AndroidAppContext
import java.io.File
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

actual class TokenAudioPlayer actual constructor() {

    actual suspend fun play(wavBytes: ByteArray): Unit = suspendCancellableCoroutine { cont ->
        val context = AndroidAppContext.application
            ?: run {
                cont.resumeWithException(IllegalStateException("AndroidAppContext not initialised"))
                return@suspendCancellableCoroutine
            }
        val file = File(context.cacheDir, "unlock-token.wav").apply {
            writeBytes(wavBytes)
        }
        val player = MediaPlayer().apply {
            setAudioAttributes(
                AudioAttributes.Builder()
                    .setUsage(AudioAttributes.USAGE_MEDIA)
                    .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
                    .build()
            )
            setDataSource(file.absolutePath)
        }
        player.setOnCompletionListener {
            try { player.release() } catch (_: Throwable) {}
            if (cont.isActive) cont.resume(Unit)
        }
        player.setOnErrorListener { _, what, extra ->
            try { player.release() } catch (_: Throwable) {}
            if (cont.isActive) {
                cont.resumeWithException(RuntimeException("MediaPlayer error what=$what extra=$extra"))
            }
            true
        }
        cont.invokeOnCancellation {
            try { player.release() } catch (_: Throwable) {}
        }
        try {
            player.prepare()
            player.start()
        } catch (t: Throwable) {
            try { player.release() } catch (_: Throwable) {}
            if (cont.isActive) cont.resumeWithException(t)
        }
    }
}
