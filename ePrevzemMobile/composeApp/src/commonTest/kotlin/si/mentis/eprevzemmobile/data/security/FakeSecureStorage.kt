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

/** Deterministic crypto so staging→commit plumbing is verifiable without real crypto. */
class FakeSecurityCrypto : SecurityCryptoPort {
    override fun generateEcdsaKeyPair(): EcdsaKeyPair =
        EcdsaKeyPair(publicKeyPem = "PUB", privateKeyBytes = "PRIV".encodeToByteArray())
    override fun randomBytes(size: Int): ByteArray = ByteArray(size) { 1 }
    override fun deriveAesKey(pin: String, salt: ByteArray): ByteArray = ("AES:$pin").encodeToByteArray()
    override fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload =
        EncryptedPayload(ciphertext = plaintext, nonce = "NONCE".encodeToByteArray())
    override fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray = payload.ciphertext
    override fun signEcdsa(privateKey: ByteArray, challenge: ByteArray): ByteArray =
        ("SIG:" + challenge.decodeToString()).encodeToByteArray()
}

class FakeBiometricGate(var result: Boolean = true) : BiometricGate {
    override suspend fun authenticate(reason: String): Boolean = result
}
