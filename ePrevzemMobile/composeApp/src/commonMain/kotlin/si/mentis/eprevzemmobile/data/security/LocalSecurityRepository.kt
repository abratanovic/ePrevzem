package si.mentis.eprevzemmobile.data.security

class LocalSecurityRepository(
    private val crypto: SecurityCryptoPort = DefaultSecurityCrypto(),
    private val storage: SecurityKeyStore = SecureStorageKeyStore(),
    private val biometricAuthenticator: BiometricGate = DefaultBiometricGate(),
) : SecurityRepository {

    private fun key(accountId: String, suffix: String) = "security.$accountId.$suffix"

    override suspend fun isRegistered(accountId: String): Boolean = runCatching {
        storage.readString(key(accountId, SUFFIX_PUBLIC)) != null &&
            storage.readString(key(accountId, SUFFIX_CIPHERTEXT)) != null &&
            storage.readString(key(accountId, SUFFIX_NONCE)) != null &&
            storage.readString(key(accountId, SUFFIX_SALT)) != null
    }.getOrElse { false }

    override suspend fun isBiometricEnabled(accountId: String): Boolean = runCatching {
        storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC)) != null
    }.getOrElse { false }

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> = runCatching {
        val keyPair = crypto.generateEcdsaKeyPair()
        val salt = crypto.randomBytes(size = 16)
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val encryptedPrivateKey = crypto.encryptAesGcm(aesKey, keyPair.privateKeyBytes)

        storage.writeString(key(STAGING_ID, SUFFIX_PUBLIC), keyPair.publicKeyPem)
        storage.writeString(key(STAGING_ID, SUFFIX_SALT), salt.toBase64())
        storage.writeString(key(STAGING_ID, SUFFIX_CIPHERTEXT), encryptedPrivateKey.ciphertext.toBase64())
        storage.writeString(key(STAGING_ID, SUFFIX_NONCE), encryptedPrivateKey.nonce.toBase64())

        if (biometricEnabled) {
            val enrolled = runCatching {
                val authenticated = biometricAuthenticator.authenticate(
                    "Aktivirajte biometrično zaščito za ePrevzem"
                )
                if (authenticated) {
                    storage.writeBiometricString(key(STAGING_ID, SUFFIX_BIOMETRIC), aesKey.toBase64())
                }
                authenticated
            }.getOrElse { false }
            if (!enrolled) {
                storage.remove(key(STAGING_ID, SUFFIX_BIOMETRIC))
            }
        } else {
            storage.remove(key(STAGING_ID, SUFFIX_BIOMETRIC))
        }

        keyPair.publicKeyPem
    }

    override suspend fun commitRegistration(accountId: String): Result<Unit> = runCatching {
        val pub = storage.readString(key(STAGING_ID, SUFFIX_PUBLIC)) ?: throw SecurityNotRegisteredException()
        val cipher = storage.readString(key(STAGING_ID, SUFFIX_CIPHERTEXT)) ?: throw SecurityNotRegisteredException()
        val nonce = storage.readString(key(STAGING_ID, SUFFIX_NONCE)) ?: throw SecurityNotRegisteredException()
        val salt = storage.readString(key(STAGING_ID, SUFFIX_SALT)) ?: throw SecurityNotRegisteredException()
        val bio = storage.readBiometricString(key(STAGING_ID, SUFFIX_BIOMETRIC))

        storage.writeString(key(accountId, SUFFIX_PUBLIC), pub)
        storage.writeString(key(accountId, SUFFIX_CIPHERTEXT), cipher)
        storage.writeString(key(accountId, SUFFIX_NONCE), nonce)
        storage.writeString(key(accountId, SUFFIX_SALT), salt)
        if (bio != null) {
            storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), bio)
        } else {
            storage.remove(key(accountId, SUFFIX_BIOMETRIC))
        }
        storage.removeAll("security.$STAGING_ID")
    }

    override suspend fun discardStaging() {
        storage.removeAll("security.$STAGING_ID")
    }

    override suspend fun enableBiometric(accountId: String, pin: String): Result<Unit> = runCatching {
        val salt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        decryptStoredPrivateKey(accountId, aesKey)

        val authenticated = biometricAuthenticator.authenticate(
            "Aktivirajte biometrično zaščito za ePrevzem"
        )
        if (!authenticated) throw BiometricAuthenticationException()

        storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), aesKey.toBase64())
    }.recoverCatching { error ->
        when (error) {
            is SecurityNotRegisteredException,
            is BiometricAuthenticationException -> throw error
            else -> throw InvalidPinException()
        }
    }

    override suspend fun disableBiometric(accountId: String): Result<Unit> = runCatching {
        storage.remove(key(accountId, SUFFIX_BIOMETRIC))
    }

    override suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit> = runCatching {
        val currentSalt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val currentAesKey = crypto.deriveAesKey(pin = currentPin, salt = currentSalt)
        val privateKey = decryptStoredPrivateKey(accountId, currentAesKey)
        val biometricEnabled = storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC)) != null

        val newSalt = crypto.randomBytes(size = 16)
        val newAesKey = crypto.deriveAesKey(pin = newPin, salt = newSalt)
        val encryptedPrivateKey = crypto.encryptAesGcm(newAesKey, privateKey)

        storage.writeString(key(accountId, SUFFIX_SALT), newSalt.toBase64())
        storage.writeString(key(accountId, SUFFIX_CIPHERTEXT), encryptedPrivateKey.ciphertext.toBase64())
        storage.writeString(key(accountId, SUFFIX_NONCE), encryptedPrivateKey.nonce.toBase64())
        if (biometricEnabled) {
            storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), newAesKey.toBase64())
        }
    }.recoverCatching { error ->
        if (error is SecurityNotRegisteredException) throw error
        throw InvalidPinException()
    }

    override suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray> = runCatching {
        val salt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val privateKey = decryptStoredPrivateKey(accountId, aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }.recoverCatching { error ->
        if (error is SecurityNotRegisteredException) throw error
        throw InvalidPinException()
    }

    override suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray> = runCatching {
        val authenticated = biometricAuthenticator.authenticate("Potrdite identiteto za podpis izziva")
        if (!authenticated) throw BiometricAuthenticationException()

        val aesKey = storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC))?.fromBase64()
            ?: throw BiometricAuthenticationException()
        val privateKey = decryptStoredPrivateKey(accountId, aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }

    private suspend fun decryptStoredPrivateKey(accountId: String, aesKey: ByteArray): ByteArray {
        val ciphertext = storage.readString(key(accountId, SUFFIX_CIPHERTEXT))?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        val nonce = storage.readString(key(accountId, SUFFIX_NONCE))?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        return crypto.decryptAesGcm(aesKey, EncryptedPayload(ciphertext, nonce))
    }

    override suspend fun reset(accountId: String) {
        storage.removeAll("security.$accountId")
    }
}
