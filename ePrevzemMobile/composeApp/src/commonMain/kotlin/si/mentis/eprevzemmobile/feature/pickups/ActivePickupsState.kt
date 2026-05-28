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
    val activeTab: ActiveTab = ActiveTab.Pickups,
    val auditLogEntries:List<AuditLogEntry> = emptyList(),
    val isBiometricEnabled: Boolean = false,
    val areNotificationsEnabled: Boolean = false,
    val isBiometricPinSheetVisible: Boolean = false,
    val biometricPin: String = "",
    val isUpdatingSettings: Boolean = false,
    val settingsError: String? = null,
) {
    val canConfirmBiometric: Boolean get() = biometricPin.length == BIOMETRIC_PIN_LENGTH

    companion object {
        const val BIOMETRIC_PIN_LENGTH = 6
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
