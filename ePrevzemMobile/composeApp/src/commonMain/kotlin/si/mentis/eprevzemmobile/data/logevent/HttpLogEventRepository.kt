package si.mentis.eprevzemmobile.data.logevent

import io.ktor.client.call.body
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.logevent.dto.AuditLogEntryDto
import si.mentis.eprevzemmobile.data.logevent.dto.toDomain
import si.mentis.eprevzemmobile.domain.AppUser

/**
 * Loads audit-log entries for the signed-in user from the backend. The endpoint is chosen
 * from the active profile: operators (employees) get their personal work feed, everyone else
 * gets the citizen feed. Auth (per-account bearer token + 401 refresh) is handled by
 * [ApiClient.authorizedGet]. Failures degrade to an empty list so the non-critical feed never
 * crashes the screen (which offers a manual refresh).
 */
class HttpLogEventRepository(
    private val api: ApiClient,
    private val activeProfile: suspend () -> AppUser?,
) : LogEventRepository {

    override suspend fun getLogEventsForCurrentUser(): List<LogEvent> {
        val path = if (activeProfile() is AppUser.Employee) OPERATOR_PATH else CITIZEN_PATH
        return try {
            val response = api.authorizedGet(path)
            if (response.status.value !in 200..299) {
                emptyList()
            } else {
                response.body<List<AuditLogEntryDto>>().mapNotNull { it.toDomain() }
            }
        } catch (e: Exception) {
            emptyList()
        }
    }

    private companion object {
        const val CITIZEN_PATH = "/api/citizen/audit-log"
        const val OPERATOR_PATH = "/api/operator/audit-log"
    }
}
