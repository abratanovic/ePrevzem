package si.mentis.eprevzemmobile.data.security

class FakeSecurityKeyStore : SecurityKeyStore {
    val plain = mutableMapOf<String, String>()
    val biometric = mutableMapOf<String, String>()

    override suspend fun readString(key: String): String? = plain[key]
    override suspend fun writeString(key: String, value: String) { plain[key] = value }
    override suspend fun remove(key: String) { plain.remove(key) }
    override suspend fun readBiometricString(key: String): String? = biometric[key]
    override suspend fun writeBiometricString(key: String, value: String) { biometric[key] = value }
    override suspend fun removeAll(prefix: String) {
        plain.keys.filter { it.startsWith("$prefix.") }.forEach { plain.remove(it) }
        biometric.keys.filter { it.startsWith("$prefix.") }.forEach { biometric.remove(it) }
    }
}

/**
 * Deterministic crypto so staging→commit plumbing is verifiable without real crypto.
 * Models AES-GCM's key binding: decryption fails (throws) when the AES key used to
 * decrypt differs from the one used to encrypt — i.e. when the wrong PIN was entered.
 */
class FakeSecurityCrypto : SecurityCryptoPort {
    override fun generateEcdsaKeyPair(): EcdsaKeyPair =
        EcdsaKeyPair(publicKeyPem = "PUB", privateKeyBytes = "PRIV".encodeToByteArray())
    override fun randomBytes(size: Int): ByteArray = ByteArray(size) { 1 }
    override fun deriveAesKey(pin: String, salt: ByteArray): ByteArray = ("AES:$pin").encodeToByteArray()

    // Prepend the AES key to the ciphertext so decrypt can verify it (stand-in for the auth tag).
    override fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload =
        EncryptedPayload(ciphertext = key + plaintext, nonce = "NONCE".encodeToByteArray())

    override fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray {
        val prefix = payload.ciphertext.copyOfRange(0, key.size)
        require(prefix.contentEquals(key)) { "AES-GCM auth failure: wrong key" }
        return payload.ciphertext.copyOfRange(key.size, payload.ciphertext.size)
    }

    override fun signEcdsa(privateKey: ByteArray, challenge: ByteArray): ByteArray =
        ("SIG:" + challenge.decodeToString()).encodeToByteArray()
}

class FakeBiometricGate(var result: Boolean = true) : BiometricGate {
    override suspend fun authenticate(reason: String): Boolean = result
}
