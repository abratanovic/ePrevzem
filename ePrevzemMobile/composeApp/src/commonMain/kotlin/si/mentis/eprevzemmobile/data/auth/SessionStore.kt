package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.StateFlow
import si.mentis.eprevzemmobile.domain.AppUser

interface SessionStore {
    val session: StateFlow<AuthSession>
    suspend fun hydrate()
    suspend fun setAuthenticated(user: AppUser)
    suspend fun clear()
    suspend fun forgetIdentity()
    suspend fun persistedUser(): AppUser?
}
