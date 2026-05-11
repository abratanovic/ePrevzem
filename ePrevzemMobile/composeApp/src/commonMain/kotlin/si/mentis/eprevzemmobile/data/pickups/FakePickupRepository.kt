package si.mentis.eprevzemmobile.data.pickups

import kotlinx.coroutines.delay
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

class FakePickupRepository : PickupRepository {

    private val pickups = listOf(
        PickupItem(
            id = "1",
            title = "Osebna izkaznica",
            organization = "Upravna enota Ljubljana",
            location = "BTC City, Ljubljana",
            lockerNumber = "Paketnik #12",
            deadline = "15. 5. 2026",
            status = EPickupStatus.Ready,
            isExpiringSoon = false,
        ),
        PickupItem(
            id = "2",
            title = "Diploma",
            organization = "Univerza v Ljubljani",
            location = "Kongresni trg, Ljubljana",
            lockerNumber = "Paketnik #7",
            deadline = "12. 5. 2026",
            status = EPickupStatus.Expiring,
            isExpiringSoon = true,
        ),
        PickupItem(
            id = "3",
            title = "Potrdilo o stalnem bivališču",
            organization = "Mestna občina Ljubljana",
            location = "Magistrat, Ljubljana",
            lockerNumber = "Paketnik #3",
            deadline = "20. 5. 2026",
            status = EPickupStatus.Ready,
            isExpiringSoon = false,
        ),
    )

    private val details = mapOf(
        "1" to PickupDetails(
            id = "1",
            title = "Osebna izkaznica",
            organization = "Upravna enota Ljubljana",
            reference = "UE-LJ-2026-0042",
            type = "Identifikacijski dokument",
            availableFrom = "8. 5. 2026",
            deadline = "2026-05-15",
            deadlineFormatted = "15. 5. 2026",
            status = EPickupStatus.Ready,
            isExpiringSoon = false,
            locationName = "BTC City, Ljubljana",
            locationAddress = "Šmartinska cesta 152, 1000 Ljubljana",
            lockerNumber = "Paketnik #12",
        ),
        "2" to PickupDetails(
            id = "2",
            title = "Diploma",
            organization = "Univerza v Ljubljani",
            reference = "UL-2026-1234",
            type = "Izobraževalni dokument",
            availableFrom = "5. 5. 2026",
            deadline = "2026-05-12",
            deadlineFormatted = "12. 5. 2026",
            status = EPickupStatus.Expiring,
            isExpiringSoon = true,
            locationName = "Kongresni trg, Ljubljana",
            locationAddress = "Kongresni trg 12, 1000 Ljubljana",
            lockerNumber = "Paketnik #7",
        ),
        "3" to PickupDetails(
            id = "3",
            title = "Potrdilo o stalnem bivališču",
            organization = "Mestna občina Ljubljana",
            reference = "MOL-2026-0088",
            type = "Uradno potrdilo",
            availableFrom = "10. 5. 2026",
            deadline = "2026-05-20",
            deadlineFormatted = "20. 5. 2026",
            status = EPickupStatus.Ready,
            isExpiringSoon = false,
            locationName = "Magistrat, Ljubljana",
            locationAddress = "Mestni trg 1, 1000 Ljubljana",
            lockerNumber = "Paketnik #3",
        ),
    )

    override suspend fun getActivePickups(): List<PickupItem> {
        delay(600)
        return pickups
    }

    override suspend fun getPickupDetails(id: String): PickupDetails? {
        delay(600)
        return details[id]
    }
}
