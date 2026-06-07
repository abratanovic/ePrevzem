package si.mentis.eprevzemmobile.data.security

class FakeSecurityRepository : SecurityRepository {
    private data class Cred(val pin: String, val biometric: Boolean)

    private val accounts = mutableMapOf<String, Cred>()
    private var staged: Cred? = null
    private var stagedPublicKey: String = ""

    override suspend fun isRegistered(accountId: String): Boolean = accounts.containsKey(accountId)

    override suspend fun isBiometricEnabled(accountId: String): Boolean = accounts[accountId]?.biometric ?: false

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> {
        staged = Cred(pin, biometricEnabled)
        stagedPublicKey = "-----BEGIN PUBLIC KEY-----\nfake-public-key-biometric-$biometricEnabled\n-----END PUBLIC KEY-----"
        return Result.success(stagedPublicKey)
    }

    override suspend fun commitRegistration(accountId: String): Result<Unit> {
        val cred = staged ?: return Result.failure(SecurityNotRegisteredException())
        accounts[accountId] = cred
        staged = null
        return Result.success(Unit)
    }

    override suspend fun discardStaging() {
        staged = null
    }

    override suspend fun enableBiometric(accountId: String, pin: String): Result<Unit> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (pin != cred.pin) return Result.failure(InvalidPinException())
        accounts[accountId] = cred.copy(biometric = true)
        return Result.success(Unit)
    }

    override suspend fun disableBiometric(accountId: String): Result<Unit> {
        accounts[accountId]?.let { accounts[accountId] = it.copy(biometric = false) }
        return Result.success(Unit)
    }

    override suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (currentPin != cred.pin) return Result.failure(InvalidPinException())
        accounts[accountId] = cred.copy(pin = newPin)
        return Result.success(Unit)
    }

    override suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (pin != cred.pin) return Result.failure(InvalidPinException())
        return Result.success("fake-pin-signature:${challenge.toBase64()}".encodeToByteArray())
    }

    override suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray> {
        if (!accounts.containsKey(accountId)) return Result.failure(SecurityNotRegisteredException())
        return Result.success("fake-biometric-signature:${challenge.toBase64()}".encodeToByteArray())
    }

    override suspend fun reset(accountId: String) {
        accounts.remove(accountId)
    }
}
