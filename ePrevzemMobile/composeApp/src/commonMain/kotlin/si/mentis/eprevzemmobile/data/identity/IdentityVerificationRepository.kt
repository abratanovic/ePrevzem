package si.mentis.eprevzemmobile.data.identity

interface IdentityVerificationRepository {
    suspend fun verifyAndRegister(
        selfieBytes: ByteArray,
        idBytes: ByteArray,
        variant: String,
    ): Result<String>
}
