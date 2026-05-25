package si.mentis.eprevzemmobile.data.security

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import si.mentis.eprevzemmobile.AndroidAppContext

private const val PREFS_NAME = "eprevzem_secure_storage"
private const val BIOMETRIC_PREFS_NAME = "eprevzem_biometric_secure_storage"
private const val BIOMETRIC_PREFIX = "biometric."
private const val BIOMETRIC_AUTH_VALID_SECONDS = 30

actual class SecureStorage actual constructor() {
    private val context: Context
        get() = checkNotNull(AndroidAppContext.application) {
            "Android application context is not initialized"
        }

    private val prefs by lazy {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        EncryptedSharedPreferences.create(
            context,
            PREFS_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
    }

    private val biometricPrefs by lazy {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .setUserAuthenticationRequired(true, BIOMETRIC_AUTH_VALID_SECONDS)
            .build()

        EncryptedSharedPreferences.create(
            context,
            BIOMETRIC_PREFS_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
    }

    actual suspend fun readString(key: String): String? =
        prefs.getString(key, null)

    actual suspend fun writeString(key: String, value: String) {
        prefs.edit().putString(key, value).apply()
    }

    actual suspend fun remove(key: String) {
        prefs.edit().remove(key).remove(BIOMETRIC_PREFIX + key).apply()
        biometricPrefs.edit().remove(BIOMETRIC_PREFIX + key).apply()
    }

    actual suspend fun readBiometricString(key: String): String? =
        biometricPrefs.getString(BIOMETRIC_PREFIX + key, null)

    actual suspend fun writeBiometricString(key: String, value: String) {
        biometricPrefs.edit().putString(BIOMETRIC_PREFIX + key, value).apply()
    }
}
