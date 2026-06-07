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
    val canSubmit: Boolean get() = rawCodeLength > 0 && !isLoading
    val hasError: Boolean get() = errorTitle != null
}
