package si.mentis.eprevzemmobile.data.pickups

import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

interface PickupRepository {
    suspend fun getActivePickups(): List<PickupItem>
    suspend fun getPickupDetails(id: String): PickupDetails?
}
