package si.mentis.eprevzemmobile.domain

import kotlinx.serialization.Serializable

@Serializable
sealed interface AppUser {
    val id: String
    val fullName: String
    val isBiometricEnabled: Boolean

    @Serializable
    data class RegularUser(
        override val id: String,
        override val fullName: String,
        val email: String,
        val phone: String,
        override val isBiometricEnabled: Boolean = false,
    ) : AppUser

    @Serializable
    data class Employee(
        override val id: String,
        override val fullName: String,
        val email: String,
        val phone: String,
        val status: String,
        val validUntil: String,
        val organizationId: String,
        val organizationName: String,
        val organizationType: String,
        val organizationLocation: String,
        val roles: List<EmployeeRole>,
        override val isBiometricEnabled: Boolean = false,
    ) : AppUser
}

@Serializable
enum class EmployeeRole { RecordManager, Operator }
