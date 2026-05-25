package si.mentis.eprevzemmobile.data.security

private const val KEY_PUBLIC_KEY = "security.public_key_pem"
private const val KEY_PRIVATE_KEY_CIPHERTEXT = "security.private_key_ciphertext"
private const val KEY_PRIVATE_KEY_NONCE = "security.private_key_nonce"
private const val KEY_PIN_SALT = "security.pin_salt"
private const val KEY_BIOMETRIC_AES_KEY = "security.biometric_aes_key"

class LocalSecurityRepository(
    private val crypto: SecurityCrypto = SecurityCrypto(),
    private val storage: SecureStorage = SecureStorage(),
    private val biometricAuthenticator: BiometricAuthenticator = BiometricAuthenticator(),
) : SecurityRepository {

    override suspend fun isRegistered(): Boolean =
        storage.readString(KEY_PUBLIC_KEY) != null &&
            storage.readString(KEY_PRIVATE_KEY_CIPHERTEXT) != null &&
            storage.readString(KEY_PRIVATE_KEY_NONCE) != null &&
            storage.readString(KEY_PIN_SALT) != null

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> = runCatching {
        val keyPair = crypto.generateEcdsaKeyPair()
        val salt = crypto.randomBytes(size = 16)
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val encryptedPrivateKey = crypto.encryptAesGcm(aesKey, keyPair.privateKeyBytes)

        storage.writeString(KEY_PUBLIC_KEY, keyPair.publicKeyPem)
        storage.writeString(KEY_PIN_SALT, salt.toBase64())
        storage.writeString(KEY_PRIVATE_KEY_CIPHERTEXT, encryptedPrivateKey.ciphertext.toBase64())
        storage.writeString(KEY_PRIVATE_KEY_NONCE, encryptedPrivateKey.nonce.toBase64())

        if (biometricEnabled) {
            val enrolled = runCatching {
                val authenticated = biometricAuthenticator.authenticate(
                    "Aktivirajte biometrično zaščito za ePrevzem"
                )
                if (authenticated) {
                    storage.writeBiometricString(KEY_BIOMETRIC_AES_KEY, aesKey.toBase64())
                }
                authenticated
            }.getOrElse { false }
            if (!enrolled) {
                storage.remove(KEY_BIOMETRIC_AES_KEY)
            }
        } else {
            storage.remove(KEY_BIOMETRIC_AES_KEY)
        }

        keyPair.publicKeyPem
    }

    override suspend fun signChallengeWithPin(pin: String, challenge: ByteArray): Result<ByteArray> = runCatching {
        val salt = storage.readString(KEY_PIN_SALT)?.fromBase64() ?: throw SecurityNotRegisteredException()
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val privateKey = decryptStoredPrivateKey(aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }.recoverCatching { error ->
        if (error is SecurityNotRegisteredException) throw error
        throw InvalidPinException()
    }

    override suspend fun signChallengeWithBiometric(challenge: ByteArray): Result<ByteArray> = runCatching {
        val authenticated = biometricAuthenticator.authenticate("Potrdite identiteto za podpis izziva")
        if (!authenticated) throw BiometricAuthenticationException()

        val aesKey = storage.readBiometricString(KEY_BIOMETRIC_AES_KEY)?.fromBase64()
            ?: throw BiometricAuthenticationException()
        val privateKey = decryptStoredPrivateKey(aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }

    private suspend fun decryptStoredPrivateKey(aesKey: ByteArray): ByteArray {
        val ciphertext = storage.readString(KEY_PRIVATE_KEY_CIPHERTEXT)?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        val nonce = storage.readString(KEY_PRIVATE_KEY_NONCE)?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        return crypto.decryptAesGcm(aesKey, EncryptedPayload(ciphertext, nonce))
    }
}
