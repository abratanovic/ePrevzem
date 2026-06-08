package si.mentis.eprevzemmobile.data.auth

import si.mentis.eprevzemmobile.data.security.SecureStorage

interface SessionStorage {
    suspend fun read(key: String): String?
    suspend fun write(key: String, value: String)
    suspend fun remove(key: String)
}

class SecureSessionStorage(
    private val secureStorage: SecureStorage = SecureStorage(),
) : SessionStorage {
    override suspend fun read(key: String): String? = secureStorage.readString(key)

    override suspend fun write(key: String, value: String) {
        secureStorage.writeString(key, value)
    }

    override suspend fun remove(key: String) {
        secureStorage.remove(key)
    }
}
