package si.mentis.eprevzemmobile.feature.pickups

sealed interface ActivePickupsEvent {
    data object Refresh : ActivePickupsEvent
    data class PickupClicked(val id: String) : ActivePickupsEvent
    data class TabSelected(val tab: ActiveTab) : ActivePickupsEvent
}
