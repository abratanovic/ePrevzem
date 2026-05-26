package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.data.security.SecureStorage
import si.mentis.eprevzemmobile.domain.AppUser

private const val KEY_PERSISTED_USER = "auth.persisted_user"

class PersistedSessionStore(
    private val storage: SecureStorage = SecureStorage(),
    private val json: Json = DefaultJson,
) : SessionStore {

    private val _session = MutableStateFlow<AuthSession>(AuthSession.Unknown)
    override val session: StateFlow<AuthSession> = _session.asStateFlow()

    override suspend fun hydrate() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun setAuthenticated(user: AppUser) {
        storage.writeString(KEY_PERSISTED_USER, json.encodeToString(AppUser.serializer(), user))
        _session.value = AuthSession.Authenticated(user)
    }

    override suspend fun clear() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun forgetIdentity() {
        storage.remove(KEY_PERSISTED_USER)
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun persistedUser(): AppUser? {
        val raw = storage.readString(KEY_PERSISTED_USER) ?: return null
        return runCatching { json.decodeFromString(AppUser.serializer(), raw) }.getOrNull()
    }

    private companion object {
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
