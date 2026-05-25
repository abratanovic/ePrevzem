package si.mentis.eprevzemmobile.data.security

expect class BiometricAuthenticator() {
    suspend fun authenticate(reason: String): Boolean
}
