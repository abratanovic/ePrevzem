package si.mentis.eprevzemmobile.data.security

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import com.russhwolf.settings.SharedPreferencesSettings
import com.russhwolf.settings.Settings
import si.mentis.eprevzemmobile.AndroidAppContext

private const val PREFS_NAME = "eprevzem_secure_storage"
private const val BIOMETRIC_PREFIX = "biometric."

actual class SecureStorage actual constructor() {
    private val context: Context
        get() = checkNotNull(AndroidAppContext.application) {
            "Android application context is not initialized"
        }

    private val settings: Settings by lazy {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        val prefs = EncryptedSharedPreferences.create(
            context,
            PREFS_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
        SharedPreferencesSettings(prefs)
    }

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
