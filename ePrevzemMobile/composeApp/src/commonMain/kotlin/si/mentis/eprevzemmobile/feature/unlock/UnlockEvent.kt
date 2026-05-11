package si.mentis.eprevzemmobile.feature.unlock

sealed interface UnlockEvent {
    data object Back : UnlockEvent
    data object RequestPermission : UnlockEvent
    data object PermissionGranted : UnlockEvent
    data object PermissionDenied : UnlockEvent
    data object OpenSettings : UnlockEvent
    data class QrDetected(val raw: String) : UnlockEvent
    data object DismissScanError : UnlockEvent
    data object Retry : UnlockEvent
    data object ContactSupport : UnlockEvent
}
