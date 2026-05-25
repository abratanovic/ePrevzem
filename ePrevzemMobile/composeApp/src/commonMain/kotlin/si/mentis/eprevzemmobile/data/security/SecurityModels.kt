package si.mentis.eprevzemmobile.data.security

data class EcdsaKeyPair(
    val publicKeyPem: String,
    val privateKeyBytes: ByteArray,
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is EcdsaKeyPair) return false
        return publicKeyPem == other.publicKeyPem && privateKeyBytes.contentEquals(other.privateKeyBytes)
    }

    override fun hashCode(): Int = 31 * publicKeyPem.hashCode() + privateKeyBytes.contentHashCode()
}

data class EncryptedPayload(
    val ciphertext: ByteArray,
    val nonce: ByteArray,
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is EncryptedPayload) return false
        return ciphertext.contentEquals(other.ciphertext) && nonce.contentEquals(other.nonce)
    }

    override fun hashCode(): Int = 31 * ciphertext.contentHashCode() + nonce.contentHashCode()
}

class SecurityNotRegisteredException : Exception("Device is not registered")
class InvalidPinException : Exception("PIN is invalid")
class BiometricAuthenticationException : Exception("Biometric authentication failed")
