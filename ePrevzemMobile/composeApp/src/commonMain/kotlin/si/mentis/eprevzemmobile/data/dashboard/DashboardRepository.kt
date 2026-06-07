package si.mentis.eprevzemmobile.data.dashboard

import si.mentis.eprevzemmobile.feature.operator.OperatorDashboardStats

interface DashboardRepository {
    /**
     * Organization-wide counters for the operator home overview. Returns null
     * when the call fails so the UI can keep its current values rather than
     * crashing.
     */
    suspend fun getStats(): OperatorDashboardStats?
}
