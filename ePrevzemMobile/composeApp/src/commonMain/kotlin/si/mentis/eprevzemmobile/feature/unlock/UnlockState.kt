package si.mentis.eprevzemmobile.feature.unlock

import androidx.compose.runtime.Immutable

@Immutable
data class UnlockState(
    val pickupId: String,
    val expectedLockerNumber: String,
    val phase: UnlockPhase,
    val attempt: Int = 0,
) {
    companion object {
        const val MAX_ATTEMPTS: Int = 3
    }
}

sealed interface UnlockPhase {
    data object RequestingPermission : UnlockPhase
    data object Scanning : UnlockPhase
    data class ScanError(val reason: ScanErrorReason) : UnlockPhase
    data object Unlocking : UnlockPhase
    data class Failed(val error: UnlockError) : UnlockPhase
    data object Unlocked : UnlockPhase
}

enum class ScanErrorReason { InvalidPayload, WrongLocker, CameraDenied }

sealed interface UnlockError {
    data object Network : UnlockError
    data class Api(val errorNumber: Int) : UnlockError
    data object PlaybackFailed : UnlockError
}
