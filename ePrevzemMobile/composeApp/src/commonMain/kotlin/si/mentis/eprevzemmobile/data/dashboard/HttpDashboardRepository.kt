package si.mentis.eprevzemmobile.data.dashboard

import io.ktor.client.call.body
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.DashboardStatsDto
import si.mentis.eprevzemmobile.feature.operator.OperatorDashboardStats

/**
 * Talks to the org dashboard API (`/api/org/dashboard/stats`) via
 * [ApiClient.authorizedGet], which attaches the employee bearer token and
 * refreshes it on expiry. Any failure degrades to null so the operator home
 * keeps its current counters instead of crashing.
 */
class HttpDashboardRepository(
    private val api: ApiClient,
) : DashboardRepository {

    override suspend fun getStats(): OperatorDashboardStats? {
        return try {
            val response = api.authorizedGet("/api/org/dashboard/stats")
            if (response.status.value !in 200..299) {
                return null
            }
            response.body<DashboardStatsDto>().toStats()
        } catch (e: Exception) {
            null
        }
    }

    private fun DashboardStatsDto.toStats(): OperatorDashboardStats = OperatorDashboardStats(
        pendingInsertionCount = awaitingPlacement,
        inLockerCount = pendingPickups,
        expiredCount = expiredThisWeek,
    )
}
