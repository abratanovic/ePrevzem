package si.mentis.eprevzemmobile.data.auth

import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import kotlin.random.Random

@OptIn(ExperimentalEncodingApi::class)
class DeviceSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
) {
    suspend fun saveSession(
        deviceId: String,
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(deviceIdKey(deviceId), deviceId)
        storage.write(accessTokenKey(deviceId), accessToken)
        storage.write(accessExpiresKey(deviceId), accessExpiresAt)
        storage.write(refreshTokenKey(deviceId), refreshToken)
    }

    suspend fun deviceId(accountId: String): String? = storage.read(deviceIdKey(accountId))

    suspend fun accessToken(accountId: String): String? = storage.read(accessTokenKey(accountId))

    suspend fun refreshToken(accountId: String): String? = storage.read(refreshTokenKey(accountId))

    suspend fun fingerprint(): String {
        val existing = storage.read(KEY_FINGERPRINT)
        if (existing != null) {
            return existing
        }
        val fingerprint = Base64.encode(Random.nextBytes(16))
        storage.write(KEY_FINGERPRINT, fingerprint)
        return fingerprint
    }

    suspend fun updateTokens(
        accountId: String,
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(accessTokenKey(accountId), accessToken)
        storage.write(accessExpiresKey(accountId), accessExpiresAt)
        storage.write(refreshTokenKey(accountId), refreshToken)
    }

    suspend fun clear(accountId: String) {
        storage.remove(deviceIdKey(accountId))
        storage.remove(accessTokenKey(accountId))
        storage.remove(accessExpiresKey(accountId))
        storage.remove(refreshTokenKey(accountId))
    }

    private companion object {
        const val KEY_FINGERPRINT = "auth.device_fingerprint"

        fun deviceIdKey(accountId: String) = "auth.$accountId.device_id"
        fun accessTokenKey(accountId: String) = "auth.$accountId.access_token"
        fun accessExpiresKey(accountId: String) = "auth.$accountId.access_expires"
        fun refreshTokenKey(accountId: String) = "auth.$accountId.refresh_token"
    }
}
