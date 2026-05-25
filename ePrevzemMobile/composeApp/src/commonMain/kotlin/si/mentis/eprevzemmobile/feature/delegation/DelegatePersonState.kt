package si.mentis.eprevzemmobile.feature.delegation

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.data.delegation.DelegatePerson

enum class DelegatePersonPhase { Search, Preview, Confirming }

@Immutable
data class DelegatePersonState(
    val phase: DelegatePersonPhase = DelegatePersonPhase.Search,
    val emso: String = "",
    val isLoading: Boolean = false,
    val foundPerson: DelegatePerson? = null,
    val errorTitle: String? = null,
    val errorMessage: String? = null,
) {
    val canSearch: Boolean get() = emso.length == EMSO_LENGTH && !isLoading
    val hasError: Boolean get() = errorTitle != null

    companion object {
        const val EMSO_LENGTH = 13
    }
}
