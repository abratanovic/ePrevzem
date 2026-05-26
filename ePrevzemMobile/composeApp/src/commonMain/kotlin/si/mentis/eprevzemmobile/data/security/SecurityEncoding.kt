package si.mentis.eprevzemmobile.data.security

private const val BASE64_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"

internal fun ByteArray.toBase64(): String {
    val output = StringBuilder((size + 2) / 3 * 4)
    var index = 0
    while (index < size) {
        val first = this[index++].toInt() and 0xff
        val second = if (index < size) this[index++].toInt() and 0xff else -1
        val third = if (index < size) this[index++].toInt() and 0xff else -1

        output.append(BASE64_ALPHABET[first ushr 2])
        output.append(BASE64_ALPHABET[((first and 0x03) shl 4) or ((second.takeIf { it >= 0 } ?: 0) ushr 4)])
        output.append(if (second >= 0) BASE64_ALPHABET[((second and 0x0f) shl 2) or ((third.takeIf { it >= 0 } ?: 0) ushr 6)] else '=')
        output.append(if (third >= 0) BASE64_ALPHABET[third and 0x3f] else '=')
    }
    return output.toString()
}

internal fun String.fromBase64(): ByteArray {
    val clean = filterNot { it.isWhitespace() }
    require(clean.length % 4 == 0) { "Invalid Base64 length" }

    val output = ArrayList<Byte>(clean.length / 4 * 3)
    var index = 0
    while (index < clean.length) {
        val first = clean[index++].base64Value()
        val second = clean[index++].base64Value()
        val thirdChar = clean[index++]
        val fourthChar = clean[index++]
        val third = if (thirdChar == '=') -1 else thirdChar.base64Value()
        val fourth = if (fourthChar == '=') -1 else fourthChar.base64Value()

        output.add(((first shl 2) or (second ushr 4)).toByte())
        if (third >= 0) {
            output.add((((second and 0x0f) shl 4) or (third ushr 2)).toByte())
        }
        if (fourth >= 0) {
            output.add((((third and 0x03) shl 6) or fourth).toByte())
        }
    }
    return output.toByteArray()
}

internal fun ByteArray.toPem(label: String): String {
    val body = toBase64().chunked(64).joinToString("\n")
    return "-----BEGIN $label-----\n$body\n-----END $label-----"
}

private fun Char.base64Value(): Int {
    val value = BASE64_ALPHABET.indexOf(this)
    require(value >= 0) { "Invalid Base64 character" }
    return value
}
