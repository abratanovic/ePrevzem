package si.mentis.eprevzemmobile.data.locker

/**
 * Platform-specific gzip decompression. Sandbox API returns the WAV token as
 * base64-encoded gzip-compressed bytes; we need a real platform implementation
 * to inflate it.
 */
internal expect fun gunzip(bytes: ByteArray): ByteArray

/** Detect the gzip magic bytes (0x1f 0x8b). */
internal fun isGzipped(bytes: ByteArray): Boolean =
    bytes.size >= 2 && bytes[0] == 0x1f.toByte() && bytes[1] == 0x8b.toByte()
