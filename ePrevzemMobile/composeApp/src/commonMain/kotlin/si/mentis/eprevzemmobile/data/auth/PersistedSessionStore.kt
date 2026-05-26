package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.domain.AppUser

class PersistedSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
    private val json: Json = DefaultJson,
) : SessionStore {

    private val _session = MutableStateFlow<AuthSession>(AuthSession.Unknown)
    override val session: StateFlow<AuthSession> = _session.asStateFlow()

    private val _profiles = MutableStateFlow<List<AppUser>>(emptyList())
    override val profiles: StateFlow<List<AppUser>> = _profiles.asStateFlow()

    override suspend fun hydrate() {
        TODO("implemented in Task 6")
    }
    override suspend fun addProfile(user: AppUser) {
        TODO("implemented in Task 6")
    }
    override suspend fun switchProfile(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun removeProfile(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun setAuthenticated(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun clear() {
        TODO("implemented in Task 6")
    }
    override suspend fun forgetAllIdentities() {
        TODO("implemented in Task 6")
    }
    override suspend fun activeProfile(): AppUser? {
        TODO("implemented in Task 6")
    }

    internal companion object {
        const val KEY_PROFILES = "auth.persisted_profiles"
        const val KEY_ACTIVE_PROFILE_ID = "auth.active_profile_id"
        const val LEGACY_KEY_PERSISTED_USER = "auth.persisted_user"
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
