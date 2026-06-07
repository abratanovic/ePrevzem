package si.mentis.eprevzemmobile.data.api

import kotlinx.serialization.Serializable

/**
 * A citizen's pickup as returned by `GET /api/citizen/pickups`. Mirrors the
 * backend `CitizenPickupResponse`. Dates arrive as ISO-8601 strings; the
 * repository formats them for display.
 */
@Serializable
data class CitizenPickupDto(
    val id: String,
    val reference: String,
    val description: String,
    val organizationName: String,
    val locationName: String,
    val locationAddress: String,
    val lockerNumber: Int? = null,
    val status: String,
    val deadlineAt: String? = null,
    val createdAt: String,
    val isExpiringSoon: Boolean = false,
)

/**
 * Full detail for a single pickup from `GET /api/citizen/pickups/{id}`.
 * Mirrors the backend `CitizenPickupDetailResponse`.
 */
@Serializable
data class CitizenPickupDetailDto(
    val id: String,
    val reference: String,
    val description: String,
    val organizationName: String,
    val locationName: String,
    val locationAddress: String,
    val lockerNumber: Int? = null,
    val status: String,
    val deadlineAt: String? = null,
    val createdAt: String,
    val pickedUpAt: String? = null,
    val isExpiringSoon: Boolean = false,
)
