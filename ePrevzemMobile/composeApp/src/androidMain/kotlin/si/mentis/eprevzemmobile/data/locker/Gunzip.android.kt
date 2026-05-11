package si.mentis.eprevzemmobile.data.locker

import java.io.ByteArrayInputStream
import java.util.zip.GZIPInputStream

internal actual fun gunzip(bytes: ByteArray): ByteArray =
    GZIPInputStream(ByteArrayInputStream(bytes)).use { it.readBytes() }
