package si.mentis.eprevzemmobile.domain

data class User(
    val id: String,
    val fullName: String,
    val email: String,
    val phone: String,
    val status: String,
    val validUntil: String,
    val organizationName: String,
    val organizationType: String,
    val organizationLocation: String,
    val isBiometricEnabled: Boolean = false,
)
