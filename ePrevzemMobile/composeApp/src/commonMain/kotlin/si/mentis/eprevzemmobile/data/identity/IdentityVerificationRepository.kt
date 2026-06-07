package si.mentis.eprevzemmobile.data.identity

interface IdentityVerificationRepository {
    suspend fun verify(
        selfieBytes: ByteArray,
        idBytes: ByteArray,
        variant: String,
    ): Result<VerifyResponseDto>

    suspend fun registerByDocument(
        emso: String,
        firstName: String,
        lastName: String,
    ): Result<String>
}
