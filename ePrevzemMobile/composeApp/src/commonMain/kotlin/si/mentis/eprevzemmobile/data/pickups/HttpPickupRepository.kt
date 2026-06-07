package si.mentis.eprevzemmobile.data.pickups

import io.ktor.client.call.body
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.CitizenPickupDetailDto
import si.mentis.eprevzemmobile.data.api.CitizenPickupDto
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

/**
 * Talks to the citizen pickup API (`/api/citizen/pickups`). All calls go
 * through [ApiClient.authorizedGet], which attaches the citizen bearer token
 * and transparently refreshes it on expiry. Failures degrade to an empty list
 * / null detail so the UI shows its empty state rather than crashing.
 */
class HttpPickupRepository(
    private val api: ApiClient,
) : PickupRepository {

    override suspend fun getActivePickups(): List<PickupItem> {
        return try {
            val response = api.authorizedGet("/api/citizen/pickups")
            if (response.status.value !in 200..299) {
                return emptyList()
            }
            response.body<List<CitizenPickupDto>>().map { it.toPickupItem() }
        } catch (e: Exception) {
            emptyList()
        }
    }

    override suspend fun getPickupDetails(id: String): PickupDetails? {
        return try {
            val response = api.authorizedGet("/api/citizen/pickups/$id")
            if (response.status.value !in 200..299) {
                return null
            }
            response.body<CitizenPickupDetailDto>().toPickupDetails()
        } catch (e: Exception) {
            null
        }
    }

    override suspend fun confirmPickup(pickupId: String): Result<Unit> {
        return try {
            val response = api.authorizedPost("/api/citizen/pickups/$pickupId/confirm-pickup")
            if (response.status.value in 200..299) {
                Result.success(Unit)
            } else {
                Result.failure(Exception("Confirm failed: ${response.status.value}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    private fun CitizenPickupDto.toPickupItem(): PickupItem = PickupItem(
        id = id,
        title = description,
        organization = organizationName,
        location = locationName,
        lockerNumber = formatLocker(lockerNumber),
        deadline = formatDate(deadlineAt),
        status = mapStatus(status, isExpiringSoon),
        isExpiringSoon = isExpiringSoon,
    )

    private fun CitizenPickupDetailDto.toPickupDetails(): PickupDetails = PickupDetails(
        id = id,
        title = description,
        organization = organizationName,
        reference = reference,
        type = "",
        availableFrom = formatDate(createdAt),
        deadline = deadlineAt ?: "",
        deadlineFormatted = formatDate(deadlineAt),
        status = mapStatus(status, isExpiringSoon),
        isExpiringSoon = isExpiringSoon,
        locationName = locationName,
        locationAddress = locationAddress,
        lockerNumber = formatLocker(lockerNumber),
        unlockedAt = formatTime(pickedUpAt),
    )
}

private fun mapStatus(status: String, isExpiringSoon: Boolean): EPickupStatus = when (status) {
    "InLocker" -> if (isExpiringSoon) EPickupStatus.Expiring else EPickupStatus.Ready
    "AwaitingPersonalPickup" -> EPickupStatus.Ready
    "AwaitingPlacement" -> EPickupStatus.Draft
    "PickedUp" -> EPickupStatus.PickedUp
    "NotPickedUp" -> EPickupStatus.Expired
    "Cancelled" -> EPickupStatus.Expired
    else -> EPickupStatus.Ready
}

private fun formatLocker(lockerNumber: Int?): String =
    lockerNumber?.let { "Predalček $it" } ?: ""

/**
 * Formats an ISO-8601 timestamp (e.g. `2026-05-15T08:00:00+00:00`) as a
 * Slovenian short date `15. 5. 2026`. Returns an empty string for null/blank
 * or unparseable input. Avoids a date-library dependency by slicing the
 * date portion directly.
 */
private fun formatDate(iso: String?): String {
    if (iso.isNullOrBlank()) return ""
    val datePart = iso.substringBefore('T')
    val parts = datePart.split('-')
    if (parts.size != 3) return ""
    val year = parts[0].toIntOrNull() ?: return ""
    val month = parts[1].toIntOrNull() ?: return ""
    val day = parts[2].toIntOrNull() ?: return ""
    return "$day. $month. $year"
}

/** Extracts `HH:mm` from an ISO-8601 timestamp; null if absent/unparseable. */
private fun formatTime(iso: String?): String? {
    if (iso.isNullOrBlank()) return null
    val timePart = iso.substringAfter('T', "")
    if (timePart.length < 5) return null
    return timePart.take(5)
}
