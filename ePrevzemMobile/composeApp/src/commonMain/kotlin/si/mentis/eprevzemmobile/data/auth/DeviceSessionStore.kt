package si.mentis.eprevzemmobile.data.auth

class DeviceSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
) {
    suspend fun saveSession(
        deviceId: String,
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(KEY_DEVICE_ID, deviceId)
        storage.write(KEY_ACCESS_TOKEN, accessToken)
        storage.write(KEY_ACCESS_EXPIRES, accessExpiresAt)
        storage.write(KEY_REFRESH_TOKEN, refreshToken)
    }

    suspend fun deviceId(): String? = storage.read(KEY_DEVICE_ID)

    suspend fun accessToken(): String? = storage.read(KEY_ACCESS_TOKEN)

    suspend fun refreshToken(): String? = storage.read(KEY_REFRESH_TOKEN)

    suspend fun updateTokens(
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(KEY_ACCESS_TOKEN, accessToken)
        storage.write(KEY_ACCESS_EXPIRES, accessExpiresAt)
        storage.write(KEY_REFRESH_TOKEN, refreshToken)
    }

    suspend fun clear() {
        storage.remove(KEY_DEVICE_ID)
        storage.remove(KEY_ACCESS_TOKEN)
        storage.remove(KEY_ACCESS_EXPIRES)
        storage.remove(KEY_REFRESH_TOKEN)
    }

    private companion object {
        const val KEY_DEVICE_ID = "auth.device_id"
        const val KEY_ACCESS_TOKEN = "auth.access_token"
        const val KEY_ACCESS_EXPIRES = "auth.access_expires"
        const val KEY_REFRESH_TOKEN = "auth.refresh_token"
    }
}
