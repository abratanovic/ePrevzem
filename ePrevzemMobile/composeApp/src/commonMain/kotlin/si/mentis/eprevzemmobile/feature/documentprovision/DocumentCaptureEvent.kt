package si.mentis.eprevzemmobile.feature.documentprovision

sealed interface DocumentCaptureEvent {
    data object CameraButtonClicked : DocumentCaptureEvent
    data object RetryClicked : DocumentCaptureEvent
    data object BackClicked : DocumentCaptureEvent
}
