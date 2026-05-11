package si.mentis.eprevzemmobile.data.locker

interface LockerRepository {
    suspend fun openBox(boxId: Long, tokenFormat: Int = 1): OpenBoxResult
}

sealed interface OpenBoxResult {
    data class Success(val tokenWavBytes: ByteArray) : OpenBoxResult
    data class ApiFailure(val errorNumber: Int) : OpenBoxResult
    data class NetworkFailure(val cause: Throwable) : OpenBoxResult
}
