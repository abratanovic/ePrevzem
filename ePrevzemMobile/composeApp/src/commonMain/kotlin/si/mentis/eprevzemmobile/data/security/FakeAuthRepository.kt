package si.mentis.eprevzemmobile.data.security

class FakeAuthRepository(
    private val crypto: SecurityCrypto = SecurityCrypto(),
) : AuthRepository {
    override suspend fun getChallenge(deviceId: String): Result<ByteArray> =
        Result.success(crypto.randomBytes(size = 32))

    override suspend fun verifySignature(deviceId: String, signature: ByteArray): Result<String> =
        Result.success(
            listOf(
                """{"alg":"ES256","typ":"JWT"}""".encodeToByteArray().toBase64(),
                """{"sub":"$deviceId","scope":"eprevzem-mobile"}""".encodeToByteArray().toBase64(),
                signature.toBase64(),
            ).joinToString("."),
        )
}
