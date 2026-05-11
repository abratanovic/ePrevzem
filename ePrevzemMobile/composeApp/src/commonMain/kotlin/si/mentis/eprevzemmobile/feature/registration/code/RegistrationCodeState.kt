package si.mentis.eprevzemmobile.feature.registration.code

import androidx.compose.runtime.Immutable

@Immutable
data class RegistrationCodeState(
    val code: String = "",
    val isLoading: Boolean = false,
    val errorTitle: String? = null,
    val errorMessage: String? = null,
    val isHelpVisible: Boolean = false,
) {
    val rawCodeLength: Int get() = code.count { it != '-' }
    val isCodeComplete: Boolean get() = rawCodeLength == CODE_LENGTH
    val canSubmit: Boolean get() = isCodeComplete && !isLoading
    val hasError: Boolean get() = errorTitle != null

    companion object {
        const val CODE_LENGTH = 9
    }
}
