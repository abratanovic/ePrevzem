package si.mentis.eprevzemmobile.feature.registration.confirm

import androidx.compose.runtime.Immutable

@Immutable
data class ConfirmAccountState(
    val account: ConfirmAccountData = ConfirmAccountData(),
    val organization: ConfirmOrganizationData = ConfirmOrganizationData(),
)

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
