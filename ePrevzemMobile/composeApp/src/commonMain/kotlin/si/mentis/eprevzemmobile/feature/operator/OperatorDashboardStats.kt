package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Immutable

/** Counters shown on the operator home "Današnji pregled" overview. */
@Immutable
data class OperatorDashboardStats(
    val pendingInsertionCount: Int,
    val inLockerCount: Int,
    val expiredCount: Int,
)
