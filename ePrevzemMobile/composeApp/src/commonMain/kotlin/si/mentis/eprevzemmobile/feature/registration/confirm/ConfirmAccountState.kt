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
    val isLoading: Boolean = false,
    val error: String? = null,
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
    val fullName: String = "",
    val email: String = "",
    val emso: String? = null,
    val phone: String = "",
    val status: String = "",
    val validUntil: String = "",
) {
    val identityLabel: String get() = if (emso.isNullOrBlank()) "E-pošta" else "EMŠO"
    val identityValue: String get() = emso?.takeIf { it.isNotBlank() } ?: email
}

@Immutable
data class ConfirmOrganizationData(
    val name: String = "",
    val type: String = "",
    val location: String = "",
)
