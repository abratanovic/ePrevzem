package si.mentis.eprevzemmobile.data.auth

import si.mentis.eprevzemmobile.domain.AppUser

sealed interface AuthSession {
    data object Unknown : AuthSession
    data object Unauthenticated : AuthSession
    data class Authenticated(val user: AppUser) : AuthSession
}
