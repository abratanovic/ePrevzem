package si.mentis.eprevzemmobile.data.security

interface SecurityRepository {
    suspend fun isRegistered(): Boolean
    suspend fun register(pin: String, biometricEnabled: Boolean): Result<String>
    suspend fun signChallengeWithPin(pin: String, challenge: ByteArray): Result<ByteArray>
    suspend fun signChallengeWithBiometric(challenge: ByteArray): Result<ByteArray>
}
