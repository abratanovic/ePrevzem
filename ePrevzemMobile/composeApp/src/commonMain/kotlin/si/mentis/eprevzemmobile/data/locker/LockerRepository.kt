package si.mentis.eprevzemmobile.data.locker

/**
 * Opens a physical locker through the ePrevzem backend (which proxies the
 * smart-locker hardware). Returns the audio token the device plays to actuate
 * the lock. The backend resolves the hardware box server-side, so the client
 * never handles a box id.
 */
interface LockerRepository {
    /** Opens the locker holding the citizen's pickup. */
    suspend fun openForPickup(pickupId: String): OpenBoxResult

    /** Opens a chosen free locker so an operator can insert a pending package. */
    suspend fun openForInsertion(packageId: String, lockerId: String): OpenBoxResult
}

sealed interface OpenBoxResult {
    data class Success(val tokenWavBytes: ByteArray) : OpenBoxResult
    data class ApiFailure(val errorNumber: Int) : OpenBoxResult
    data class NetworkFailure(val cause: Throwable) : OpenBoxResult
}
