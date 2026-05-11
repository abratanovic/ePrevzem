package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.UnlockPhase

@Immutable
data class PickupDetailsState(
    val details: PickupDetails,
    val showUnlockDialog: Boolean = false,
    val showBiometricSheet: Boolean = false,
    val showPinSheet: Boolean = false,
    val pinValue: String = "",
    val unlockPhase: UnlockPhase = UnlockPhase.Idle,
    val secondsRemaining: Int = 30,
)
