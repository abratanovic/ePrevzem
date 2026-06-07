package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.StateFlow
import si.mentis.eprevzemmobile.domain.AppUser

interface SessionStore {
    val session: StateFlow<AuthSession>
    val profiles: StateFlow<List<AppUser>>
    suspend fun hydrate()
    suspend fun addProfile(user: AppUser)
    suspend fun switchProfile(userId: String)
    suspend fun removeProfile(userId: String)
    suspend fun setAuthenticated(userId: String)
    suspend fun clear()
    suspend fun forgetAllIdentities()
    suspend fun activeProfile(): AppUser?
}
