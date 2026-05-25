package si.mentis.eprevzemmobile.feature.pickups

sealed interface PickupDetailsEvent {
    data object Back : PickupDetailsEvent
    data object Share : PickupDetailsEvent
    data object UnlockClicked : PickupDetailsEvent
    data object UnlockConfirmed : PickupDetailsEvent
    data object UnlockCancelled : PickupDetailsEvent
    data object BiometricSelected : PickupDetailsEvent
    data object PinSelected : PickupDetailsEvent
    data class PinDigitEntered(val digit: Int) : PickupDetailsEvent
    data object PinBackspace : PickupDetailsEvent
    data object Finish : PickupDetailsEvent
    data object LockerDidNotOpen : PickupDetailsEvent
    data object IdentityVerified : PickupDetailsEvent
    data object DelegatePersonClicked : PickupDetailsEvent
}
