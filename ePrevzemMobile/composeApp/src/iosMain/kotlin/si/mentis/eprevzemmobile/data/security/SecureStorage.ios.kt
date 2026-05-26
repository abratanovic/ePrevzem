package si.mentis.eprevzemmobile.data.security

import com.russhwolf.settings.ExperimentalSettingsImplementation
import com.russhwolf.settings.KeychainSettings
import com.russhwolf.settings.Settings

private const val SERVICE_NAME = "si.mentis.eprevzemmobile.secure-storage"
private const val BIOMETRIC_PREFIX = "biometric."

@OptIn(ExperimentalSettingsImplementation::class)
actual class SecureStorage actual constructor() {
    private val settings: Settings = KeychainSettings(SERVICE_NAME)

    actual suspend fun readString(key: String): String? = settings.getStringOrNull(key)

    actual suspend fun writeString(key: String, value: String) {
        settings.putString(key, value)
    }

    actual suspend fun remove(key: String) {
        settings.remove(key)
        settings.remove(BIOMETRIC_PREFIX + key)
    }

    // Biometric gating is enforced by BiometricAuthenticator at the repository layer before
    // these are called; here we only need durable storage namespaced under a separate key.
    actual suspend fun readBiometricString(key: String): String? =
        settings.getStringOrNull(BIOMETRIC_PREFIX + key)

    actual suspend fun writeBiometricString(key: String, value: String) {
        settings.putString(BIOMETRIC_PREFIX + key, value)
    }

    actual suspend fun clearAll() {
        settings.clear()
    }
}
