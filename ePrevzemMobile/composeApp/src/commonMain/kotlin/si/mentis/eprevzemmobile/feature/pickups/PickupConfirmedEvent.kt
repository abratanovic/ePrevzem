package si.mentis.eprevzemmobile.feature.pickups

sealed interface PickupConfirmedEvent {
    data object Finish : PickupConfirmedEvent
}
