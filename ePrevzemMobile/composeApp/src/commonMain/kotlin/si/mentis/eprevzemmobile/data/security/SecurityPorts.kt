package si.mentis.eprevzemmobile.data.security

/**
 * Narrow secure-storage seam used by [LocalSecurityRepository] so tests can
 * substitute an in-memory implementation. Production wraps [SecureStorage].
 */
interface SecurityKeyStore {
    suspend fun readString(key: String): String?
    suspend fun writeString(key: String, value: String)
    suspend fun remove(key: String)
    suspend fun readBiometricString(key: String): String?
    suspend fun writeBiometricString(key: String, value: String)
    suspend fun removeAll(prefix: String)
}

class SecureStorageKeyStore(
    private val storage: SecureStorage = SecureStorage(),
) : SecurityKeyStore {
    override suspend fun readString(key: String) = storage.readString(key)
    override suspend fun writeString(key: String, value: String) = storage.writeString(key, value)
    override suspend fun remove(key: String) = storage.remove(key)
    override suspend fun readBiometricString(key: String) = storage.readBiometricString(key)
    override suspend fun writeBiometricString(key: String, value: String) = storage.writeBiometricString(key, value)
    // SecureStorage has no prefix scan; remove the known suffixes for the prefix's account.
    override suspend fun removeAll(prefix: String) {
        storage.remove("$prefix.$SUFFIX_PUBLIC")
        storage.remove("$prefix.$SUFFIX_CIPHERTEXT")
        storage.remove("$prefix.$SUFFIX_NONCE")
        storage.remove("$prefix.$SUFFIX_SALT")
        storage.remove("$prefix.$SUFFIX_BIOMETRIC")
    }
}

/** Crypto seam — production wraps the [SecurityCrypto] expect class (which cannot be faked directly). */
interface SecurityCryptoPort {
    fun randomBytes(size: Int): ByteArray
    fun generateEcdsaKeyPair(): EcdsaKeyPair
    fun deriveAesKey(pin: String, salt: ByteArray): ByteArray
    fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload
    fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray
    fun signEcdsa(privateKey: ByteArray, challenge: ByteArray): ByteArray
}

class DefaultSecurityCrypto(
    private val crypto: SecurityCrypto = SecurityCrypto(),
) : SecurityCryptoPort {
    override fun randomBytes(size: Int) = crypto.randomBytes(size)
    override fun generateEcdsaKeyPair() = crypto.generateEcdsaKeyPair()
    override fun deriveAesKey(pin: String, salt: ByteArray) = crypto.deriveAesKey(pin, salt)
    override fun encryptAesGcm(key: ByteArray, plaintext: ByteArray) = crypto.encryptAesGcm(key, plaintext)
    override fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload) = crypto.decryptAesGcm(key, payload)
    override fun signEcdsa(privateKey: ByteArray, challenge: ByteArray) = crypto.signEcdsa(privateKey, challenge)
}

/** Biometric seam — production wraps the [BiometricAuthenticator] expect class. */
interface BiometricGate {
    suspend fun authenticate(reason: String): Boolean
}

class DefaultBiometricGate(
    private val authenticator: BiometricAuthenticator = BiometricAuthenticator(),
) : BiometricGate {
    override suspend fun authenticate(reason: String) = authenticator.authenticate(reason)
}

internal const val SUFFIX_PUBLIC = "public_key_pem"
internal const val SUFFIX_CIPHERTEXT = "private_key_ciphertext"
internal const val SUFFIX_NONCE = "private_key_nonce"
internal const val SUFFIX_SALT = "pin_salt"
internal const val SUFFIX_BIOMETRIC = "biometric_aes_key"
internal const val STAGING_ID = "__staging__"
