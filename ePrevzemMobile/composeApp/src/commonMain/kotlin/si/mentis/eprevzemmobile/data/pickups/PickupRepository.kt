package si.mentis.eprevzemmobile.data.pickups

import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

interface PickupRepository {
    suspend fun getActivePickups(): List<PickupItem>
    suspend fun getPickupDetails(id: String): PickupDetails?

    /** Commits a pickup after the locker was opened and the user confirmed collection. */
    suspend fun confirmPickup(pickupId: String): Result<Unit>
}
