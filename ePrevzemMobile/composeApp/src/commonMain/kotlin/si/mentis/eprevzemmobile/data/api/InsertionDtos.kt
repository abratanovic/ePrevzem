package si.mentis.eprevzemmobile.data.api

import kotlinx.serialization.Serializable

/** The audio token (base64 WAV) returned by the locker-open endpoints. */
@Serializable
data class LockerTokenDto(val tokenBase64: String)

/** Body for the operator insertion open/confirm endpoints. */
@Serializable
data class InsertionLockerRequestDto(val lockerId: String)

/** `GET /api/org/insertion/context` — station + pending packages + free lockers. */
@Serializable
data class InsertionContextDto(
    val stationId: String,
    val serialNumber: String,
    val locationName: String,
    val packages: List<InsertionPackageDto> = emptyList(),
    val freeLockers: List<FreeLockerDto> = emptyList(),
)

@Serializable
data class InsertionPackageDto(
    val id: String,
    val reference: String,
    val description: String,
    val recipientName: String,
)

@Serializable
data class FreeLockerDto(
    val lockerId: String,
    val lockerNumber: Int,
)

/** `POST /api/org/insertion/{id}/confirm` result. */
@Serializable
data class InsertionConfirmedDto(
    val packageId: String,
    val reference: String,
    val status: String,
    val deadlineAt: String? = null,
)
