package si.mentis.eprevzemmobile.feature.operator

sealed interface OperatorHomeEvent {
    data object ScanQrClicked : OperatorHomeEvent
    data class TabSelected(val tab: OperatorTab) : OperatorHomeEvent
}
