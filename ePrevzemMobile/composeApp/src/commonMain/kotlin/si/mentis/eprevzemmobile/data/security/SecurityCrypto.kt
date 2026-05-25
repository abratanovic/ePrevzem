package si.mentis.eprevzemmobile.data.security

expect class SecurityCrypto() {
    fun randomBytes(size: Int): ByteArray
    fun generateEcdsaKeyPair(): EcdsaKeyPair
    fun deriveAesKey(pin: String, salt: ByteArray, iterations: Int = 100_000): ByteArray
    fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload
    fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray
    fun signEcdsa(privateKeyBytes: ByteArray, challenge: ByteArray): ByteArray
}
