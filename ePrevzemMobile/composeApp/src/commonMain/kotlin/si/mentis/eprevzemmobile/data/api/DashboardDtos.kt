package si.mentis.eprevzemmobile.data.api

import kotlinx.serialization.Serializable

/** `GET /api/org/dashboard/stats` — organization-wide pickup counters. */
@Serializable
data class DashboardStatsDto(
    val activePickups: Int = 0,
    val activePickupsTrend: Int = 0,
    val awaitingPlacement: Int = 0,
    val pendingPickups: Int = 0,
    val pendingExpiresToday: Int = 0,
    val occupiedLockers: Int = 0,
    val totalLockers: Int = 0,
    val expiredThisWeek: Int = 0,
)
