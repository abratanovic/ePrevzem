package si.mentis.eprevzemmobile.data.insertion

import io.ktor.client.call.body
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.InsertionContextDto
import si.mentis.eprevzemmobile.data.api.InsertionLockerRequestDto

/**
 * Talks to the operator insertion API (`/api/org/insertion`). Calls go through
 * [ApiClient]'s authenticated helpers (bearer + refresh). The Operator role and
 * org claim are enforced server-side.
 */
class HttpInsertionRepository(
    private val api: ApiClient,
) : InsertionRepository {

    override suspend fun getContext(serial: String): Result<InsertionContext> {
        return try {
            val response = api.authorizedGet("/api/org/insertion/context?serial=$serial")
            if (response.status.value !in 200..299) {
                Result.failure(InsertionException(response.status.value))
            } else {
                Result.success(response.body<InsertionContextDto>().toContext())
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override suspend fun confirmInsertion(packageId: String, lockerId: String): Result<Unit> {
        return try {
            val response = api.authorizedPost(
                "/api/org/insertion/$packageId/confirm",
                InsertionLockerRequestDto(lockerId),
            )
            if (response.status.value in 200..299) {
                Result.success(Unit)
            } else {
                Result.failure(InsertionException(response.status.value))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    private fun InsertionContextDto.toContext() = InsertionContext(
        stationId = stationId,
        serialNumber = serialNumber,
        locationName = locationName,
        packages = packages.map { InsertionPackage(it.id, it.reference, it.description, it.recipientName) },
        freeLockers = freeLockers.map { InsertionLocker(it.lockerId, it.lockerNumber) },
    )
}

/** Non-success HTTP status from the insertion API (e.g. 404 unknown station, 409 locker taken). */
class InsertionException(val statusCode: Int) : Exception("Insertion API failed: $statusCode")
