package si.mentis.eprevzemmobile.data.security

class FakeSecurityRepository : SecurityRepository {
    private var registered = false
    private var biometricEnabled = false
    private var registeredPin = ""
    private var publicKeyPem = "-----BEGIN PUBLIC KEY-----\nfake-public-key\n-----END PUBLIC KEY-----"

    override suspend fun isRegistered(): Boolean = registered

    override suspend fun isBiometricEnabled(): Boolean = biometricEnabled

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> {
        registered = true
        registeredPin = pin
        this.biometricEnabled = biometricEnabled
        publicKeyPem = "-----BEGIN PUBLIC KEY-----\nfake-public-key-biometric-$biometricEnabled\n-----END PUBLIC KEY-----"
        return Result.success(publicKeyPem)
    }

    override suspend fun enableBiometric(pin: String): Result<Unit> {
        if (!registered) return Result.failure(SecurityNotRegisteredException())
        if (pin != registeredPin) return Result.failure(InvalidPinException())
        biometricEnabled = true
        return Result.success(Unit)
    }

    override suspend fun disableBiometric(): Result<Unit> {
        biometricEnabled = false
        return Result.success(Unit)
    }

    override suspend fun changePin(currentPin: String, newPin: String): Result<Unit> {
        if (!registered) return Result.failure(SecurityNotRegisteredException())
        if (currentPin != registeredPin) return Result.failure(InvalidPinException())
        registeredPin = newPin
        return Result.success(Unit)
    }

    override suspend fun signChallengeWithPin(pin: String, challenge: ByteArray): Result<ByteArray> {
        if (!registered) return Result.failure(SecurityNotRegisteredException())
        if (pin != registeredPin) return Result.failure(InvalidPinException())
        return Result.success("fake-pin-signature:${challenge.toBase64()}".encodeToByteArray())
    }

    override suspend fun signChallengeWithBiometric(challenge: ByteArray): Result<ByteArray> =
        Result.success("fake-biometric-signature:${challenge.toBase64()}".encodeToByteArray())

    override suspend fun reset() {
        registered = false
        biometricEnabled = false
        registeredPin = ""
    }
}
