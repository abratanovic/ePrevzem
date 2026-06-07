package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

enum class ActiveTab { Pickups, History, Profile }

@Immutable
data class ActivePickupsState(
    val userName: String = "",
    val profile: ProfileData = ProfileData(),
    val pickups: List<PickupItem> = emptyList(),
    val isRefreshing: Boolean = false,
    val isHistoryRefreshing: Boolean = false,
    val activeTab: ActiveTab = ActiveTab.Pickups,
    val auditLogEntries:List<AuditLogEntry> = emptyList(),
    val isBiometricEnabled: Boolean = false,
    val areNotificationsEnabled: Boolean = false,
    val isBiometricPinSheetVisible: Boolean = false,
    val biometricPin: String = "",
    val isBiometricPinVisible: Boolean = false,
    val isChangePinSheetVisible: Boolean = false,
    val currentPin: String = "",
    val newPin: String = "",
    val newPinConfirmation: String = "",
    val isCurrentPinVisible: Boolean = false,
    val isNewPinVisible: Boolean = false,
    val isNewPinConfirmationVisible: Boolean = false,
    val isChangingPin: Boolean = false,
    val pinChangeError: String? = null,
    val isUpdatingSettings: Boolean = false,
    val settingsError: String? = null,
) {
    val canConfirmBiometric: Boolean get() = biometricPin.length == BIOMETRIC_PIN_LENGTH
    val isNewPinMismatch: Boolean get() =
        newPin.length == PIN_LENGTH && newPinConfirmation.length == PIN_LENGTH && newPin != newPinConfirmation
    val canConfirmPinChange: Boolean get() =
        currentPin.length == PIN_LENGTH &&
            newPin.length == PIN_LENGTH &&
            newPinConfirmation.length == PIN_LENGTH &&
            newPin == newPinConfirmation

    companion object {
        const val PIN_LENGTH = 6
        const val BIOMETRIC_PIN_LENGTH = PIN_LENGTH
    }
}

@Immutable
data class ProfileData(
    val fullName: String = "",
    val email: String = "",
    val phone: String = "",
    val status: String = "",
    val validUntil: String = "",
    val organizationName: String = "",
    val organizationType: String = "",
    val organizationLocation: String = "",
)
