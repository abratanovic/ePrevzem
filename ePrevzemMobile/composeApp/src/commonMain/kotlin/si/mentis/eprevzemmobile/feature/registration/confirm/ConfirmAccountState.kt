package si.mentis.eprevzemmobile.feature.registration.confirm

import androidx.compose.runtime.Immutable

@Immutable
data class ConfirmAccountState(
    val account: ConfirmAccountData = ConfirmAccountData(),
    val organization: ConfirmOrganizationData = ConfirmOrganizationData(),
    val isBiometricEnabled: Boolean = true,
    val pin: String = "",
    val pinConfirmation: String = "",
    val isPinVisible: Boolean = false,
    val isPinConfirmationVisible: Boolean = false,
) {
    val isPinTooShort: Boolean get() = pin.isNotEmpty() && pin.length < PIN_LENGTH
    val isPinMismatch: Boolean get() =
        pin.length == PIN_LENGTH && pinConfirmation.length == PIN_LENGTH && pin != pinConfirmation
    val isPinValid: Boolean get() = pin.length == PIN_LENGTH && pin == pinConfirmation
    val canSubmit: Boolean get() = isPinValid

    companion object {
        const val PIN_LENGTH = 6
    }
}

@Immutable
data class ConfirmAccountData(
    val fullName: String = "Marko Horvat",
    val email: String = "marko.horvat@gov.si",
    val phone: String = "+386 41 234 567",
    val status: String = "Aktiven",
    val validUntil: String = "14. nov 2025",
)

@Immutable
data class ConfirmOrganizationData(
    val name: String = "Upravna enota Ljubljana",
    val type: String = "Javna uprava",
    val location: String = "Adamič-Lundrovo nabrežje 2, Ljubljana",
)
