package si.mentis.eprevzemmobile.data.security

expect class SecureStorage() {
    suspend fun readString(key: String): String?
    suspend fun writeString(key: String, value: String)
    suspend fun remove(key: String)
    suspend fun readBiometricString(key: String): String?
    suspend fun writeBiometricString(key: String, value: String)
    suspend fun clearAll()
}
