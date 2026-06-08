package si.mentis.eprevzemmobile.data.identity

import kotlinx.serialization.Serializable

@Serializable
data class RegisterByDocumentResponseDto(
    val firstName: String,
    val lastName: String,
    val code: String,
    val expiresAt: String,
)

@Serializable
data class ProblemDetailsDto(
    val title: String? = null,
    val detail: String? = null,
    val status: Int? = null,
)

class DocumentVerificationException(val reasons: List<String>) : Exception("Document verification failed")
