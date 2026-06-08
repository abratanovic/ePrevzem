package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.builtins.ListSerializer
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

    private var activeId: String? = null

    private val profilesSerializer = ListSerializer(AppUser.serializer())

    override suspend fun hydrate() {
        migrateLegacyIfPresent()
        _profiles.value = readProfiles()
        val storedActiveId = storage.read(KEY_ACTIVE_PROFILE_ID)
        activeId = storedActiveId?.takeIf { id -> _profiles.value.any { it.id == id } }
        if (storedActiveId != null && activeId == null) {
            storage.remove(KEY_ACTIVE_PROFILE_ID)
        }
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun addProfile(user: AppUser) {
        val updated = _profiles.value.filterNot { it.id == user.id } + user
        _profiles.value = updated
        storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, updated))
    }

    override suspend fun switchProfile(userId: String) {
        val target = _profiles.value.firstOrNull { it.id == userId }
            ?: throw IllegalArgumentException("Unknown profile id: $userId")
        activeId = userId
        storage.write(KEY_ACTIVE_PROFILE_ID, userId)
        if (_session.value is AuthSession.Authenticated) {
            _session.value = AuthSession.Authenticated(target)
        }
    }

    override suspend fun removeProfile(userId: String) {
        val updated = _profiles.value.filterNot { it.id == userId }
        _profiles.value = updated
        storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, updated))
        if (activeId == userId) {
            activeId = null
            storage.remove(KEY_ACTIVE_PROFILE_ID)
            _session.value = AuthSession.Unauthenticated
        }
    }

    override suspend fun setAuthenticated(userId: String) {
        val user = _profiles.value.firstOrNull { it.id == userId }
            ?: throw IllegalArgumentException("Unknown profile id: $userId")
        activeId = userId
        storage.write(KEY_ACTIVE_PROFILE_ID, userId)
        _session.value = AuthSession.Authenticated(user)
    }

    override suspend fun clear() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun forgetAllIdentities() {
        _profiles.value = emptyList()
        activeId = null
        storage.remove(KEY_PROFILES)
        storage.remove(KEY_ACTIVE_PROFILE_ID)
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun activeProfile(): AppUser? {
        val id = activeId ?: return null
        return _profiles.value.firstOrNull { it.id == id }
    }

    private suspend fun readProfiles(): List<AppUser> {
        val raw = storage.read(KEY_PROFILES) ?: return emptyList()
        return runCatching { json.decodeFromString(profilesSerializer, raw) }
            .getOrElse {
                storage.remove(KEY_PROFILES)
                emptyList()
            }
    }

    private suspend fun migrateLegacyIfPresent() {
        val legacy = storage.read(LEGACY_KEY_PERSISTED_USER) ?: return
        val hasProfiles = storage.read(KEY_PROFILES) != null
        val user = runCatching { json.decodeFromString(AppUser.serializer(), legacy) }.getOrNull()
        if (!hasProfiles && user != null) {
            storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, listOf(user)))
            storage.write(KEY_ACTIVE_PROFILE_ID, user.id)
        }
        storage.remove(LEGACY_KEY_PERSISTED_USER)
    }

    internal companion object {
        const val KEY_PROFILES = "auth.persisted_profiles"
        const val KEY_ACTIVE_PROFILE_ID = "auth.active_profile_id"
        const val LEGACY_KEY_PERSISTED_USER = "auth.persisted_user"
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
