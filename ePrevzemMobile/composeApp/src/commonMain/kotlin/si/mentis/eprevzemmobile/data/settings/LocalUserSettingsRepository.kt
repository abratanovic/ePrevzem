package si.mentis.eprevzemmobile.data.settings

import si.mentis.eprevzemmobile.data.security.SecureStorage

private const val KEY_NOTIFICATIONS_ENABLED = "settings.notifications_enabled"

class LocalUserSettingsRepository(
    private val storage: SecureStorage = SecureStorage(),
) : UserSettingsRepository {

    override suspend fun areNotificationsEnabled(): Boolean =
        storage.readString(KEY_NOTIFICATIONS_ENABLED)?.toBooleanStrictOrNull() ?: false

    override suspend fun setNotificationsEnabled(enabled: Boolean) {
        storage.writeString(KEY_NOTIFICATIONS_ENABLED, enabled.toString())
    }
}
