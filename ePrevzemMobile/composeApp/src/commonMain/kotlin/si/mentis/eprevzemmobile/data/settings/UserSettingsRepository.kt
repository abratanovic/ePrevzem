package si.mentis.eprevzemmobile.data.settings

interface UserSettingsRepository {
    suspend fun areNotificationsEnabled(): Boolean
    suspend fun setNotificationsEnabled(enabled: Boolean)
}
