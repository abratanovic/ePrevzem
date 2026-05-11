package si.mentis.eprevzemmobile.core.audio

actual class TokenAudioPlayer actual constructor() {
    actual suspend fun play(wavBytes: ByteArray) {
        throw NotImplementedError("iOS audio playback not yet implemented")
    }
}
