package si.mentis.eprevzemmobile.feature.documentprovision

sealed interface DocumentTypeSelectionEvent {
    data object IdCardClicked : DocumentTypeSelectionEvent
    data object DrivingLicenceClicked : DocumentTypeSelectionEvent
    data object BackClicked : DocumentTypeSelectionEvent
}
