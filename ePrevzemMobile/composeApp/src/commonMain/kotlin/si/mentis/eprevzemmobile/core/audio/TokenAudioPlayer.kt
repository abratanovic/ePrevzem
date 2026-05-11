package si.mentis.eprevzemmobile.core.audio

/**
 * Plays a WAV byte array through the device speaker and suspends until
 * playback completes. Throws on playback error.
 */
expect class TokenAudioPlayer() {
    suspend fun play(wavBytes: ByteArray)
}
