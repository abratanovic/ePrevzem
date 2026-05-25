package si.mentis.eprevzemmobile.data.security

class FakeSecurityRepository : SecurityRepository {
    private var registered = false
    private var publicKeyPem = "-----BEGIN PUBLIC KEY-----\nfake-public-key\n-----END PUBLIC KEY-----"

    override suspend fun isRegistered(): Boolean = registered

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> {
        registered = true
        publicKeyPem = "-----BEGIN PUBLIC KEY-----\nfake-public-key-biometric-$biometricEnabled\n-----END PUBLIC KEY-----"
        return Result.success(publicKeyPem)
    }

    override suspend fun signChallengeWithPin(pin: String, challenge: ByteArray): Result<ByteArray> =
        Result.success("fake-pin-signature:${challenge.toBase64()}".encodeToByteArray())

    override suspend fun signChallengeWithBiometric(challenge: ByteArray): Result<ByteArray> =
        Result.success("fake-biometric-signature:${challenge.toBase64()}".encodeToByteArray())

    override suspend fun reset() {
        registered = false
    }
}
