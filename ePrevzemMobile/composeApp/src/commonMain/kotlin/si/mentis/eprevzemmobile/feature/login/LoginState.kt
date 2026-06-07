package si.mentis.eprevzemmobile.feature.login

import androidx.compose.runtime.Immutable

enum class LoginPhase { Biometric, Pin }

@Immutable
data class LoginState(
    val phase: LoginPhase = LoginPhase.Biometric,
    val pin: String = "",
    val isLoading: Boolean = false,
    val error: String? = null,
    val isBiometricAvailable: Boolean = true,
) {
    companion object {
        const val PIN_LENGTH = 6
    }
}
