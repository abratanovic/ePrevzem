package si.mentis.eprevzemmobile.core.camera

/**
 * Extracts the locker id from a scanned QR payload.
 *
 * Direct4.me locker QR codes encode a URL such as
 * `HTTPS://B.DIRECT4.ME/02/000537/499/138/1774444011/1/64/00/` where the second
 * path segment after the host (`000537`) is the zero-padded locker id. We return
 * that segment with leading zeroes stripped (`537`).
 *
 * Anything that is not a recognised Direct4.me URL is passed through trimmed, so
 * a plain locker number scanned directly still works.
 */
fun parseLockerId(raw: String): String {
    val trimmed = raw.trim()
    if (trimmed.contains("DIRECT4.ME", ignoreCase = true)) {
        val afterScheme = trimmed.substringAfter("://", trimmed)
        // segments: [host, "02", lockerId, ...] — locker id is the second path part.
        val segments = afterScheme.split("/").filter { it.isNotEmpty() }
        segments.getOrNull(2)?.let { lockerSegment ->
            return lockerSegment.stripLeadingZeroes()
        }
    }
    return trimmed
}

private fun String.stripLeadingZeroes(): String =
    trimStart('0').ifEmpty { "0" }
