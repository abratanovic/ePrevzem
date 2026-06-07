package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.domain.AppUser

enum class AccountType { Employee, Citizen }

@Immutable
data class AccountRow(
    val id: String,
    val fullName: String,
    val type: AccountType,
    val organizationName: String?,
)

@Immutable
data class AccountPickerState(
    val accounts: List<AccountRow> = emptyList(),
)

fun List<AppUser>.toAccountRows(): List<AccountRow> = map { user ->
    when (user) {
        is AppUser.Employee -> AccountRow(
            id = user.id,
            fullName = user.fullName,
            type = AccountType.Employee,
            organizationName = user.organizationName,
        )
        is AppUser.RegularUser -> AccountRow(
            id = user.id,
            fullName = user.fullName,
            type = AccountType.Citizen,
            organizationName = null,
        )
    }
}
