package si.mentis.eprevzemmobile.data.locker

import io.ktor.client.call.body
import io.ktor.client.statement.HttpResponse
import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.InsertionLockerRequestDto
import si.mentis.eprevzemmobile.data.api.LockerTokenDto

/**
 * Opens lockers through the ePrevzem backend. The backend authorizes the
 * request, resolves the hardware box, calls the locker vendor, and returns the
 * audio token, which this repository decodes to WAV bytes for playback.
 */
@OptIn(ExperimentalEncodingApi::class)
class HttpLockerRepository(
    private val api: ApiClient,
) : LockerRepository {

    override suspend fun openForPickup(pickupId: String): OpenBoxResult =
        open { api.authorizedPost("/api/citizen/pickups/$pickupId/open") }

    override suspend fun openForInsertion(packageId: String, lockerId: String): OpenBoxResult =
        open { api.authorizedPost("/api/org/insertion/$packageId/open", InsertionLockerRequestDto(lockerId)) }

    private suspend fun open(call: suspend () -> HttpResponse): OpenBoxResult {
        return try {
            val response = call()
            if (response.status.value !in 200..299) {
                OpenBoxResult.ApiFailure(response.status.value)
            } else {
                val dto = response.body<LockerTokenDto>()
                OpenBoxResult.Success(Base64.decode(dto.tokenBase64))
            }
        } catch (e: Exception) {
            OpenBoxResult.NetworkFailure(e)
        }
    }
}
