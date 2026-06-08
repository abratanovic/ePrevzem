package si.mentis.eprevzemmobile.data.security

interface SecurityRepository {
    suspend fun isRegistered(accountId: String): Boolean
    suspend fun isBiometricEnabled(accountId: String): Boolean

    /** Generates a keypair and writes credentials to a staging namespace. Returns the public key PEM. */
    suspend fun register(pin: String, biometricEnabled: Boolean): Result<String>

    /** Promotes staged credentials to [accountId]'s namespace and clears staging. */
    suspend fun commitRegistration(accountId: String): Result<Unit>

    /** Removes any staged credentials without touching committed accounts. */
    suspend fun discardStaging()

    suspend fun enableBiometric(accountId: String, pin: String): Result<Unit>
    suspend fun disableBiometric(accountId: String): Result<Unit>
    suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit>
    suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray>
    suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray>

    /** Wipes one account's credentials. Other accounts are unaffected. */
    suspend fun reset(accountId: String)
}
