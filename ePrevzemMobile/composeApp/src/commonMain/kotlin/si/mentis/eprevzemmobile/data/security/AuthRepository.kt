package si.mentis.eprevzemmobile.data.security

interface AuthRepository {
    suspend fun getChallenge(deviceId: String): Result<ByteArray>
    suspend fun verifySignature(deviceId: String, signature: ByteArray): Result<String>
}
