package si.mentis.eprevzemmobile.feature.pickups.model

import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus

data class PickupItem(
    val id: String,
    val title: String,
    val organization: String,
    val location: String,
    val lockerNumber: String,
    val deadline: String,
    val status: EPickupStatus,
    val isExpiringSoon: Boolean,
)

data class PickupDetails(
    val id: String,
    val title: String,
    val organization: String,
    val reference: String,
    val type: String,
    val availableFrom: String,
    val deadline: String,
    val deadlineFormatted: String,
    val status: EPickupStatus,
    val isExpiringSoon: Boolean,
    val locationName: String,
    val locationAddress: String,
    val lockerNumber: String,
    val unlockedAt: String? = null,
)

enum class UnlockPhase { Idle, Unlocked, Confirmed }
