package si.mentis.eprevzemmobile.data.identity

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class VerifyResponseDto(
    val verified: Boolean,
    @SerialName("first_name") val firstName: String? = null,
    @SerialName("last_name") val lastName: String? = null,
    val emso: String? = null,
    val reasons: List<String> = emptyList(),
)

@Serializable
data class RegisterByDocumentRequestDto(
    val emso: String,
    val firstName: String,
    val lastName: String,
)

@Serializable
data class RegisterByDocumentResponseDto(
    val firstName: String,
    val lastName: String,
    val code: String,
    val expiresAt: String,
)
