package si.mentis.eprevzemmobile.data.identity

import io.ktor.client.call.body
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.timeout
import io.ktor.client.request.forms.MultiPartFormDataContent
import io.ktor.client.request.forms.formData
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.Headers
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import si.mentis.eprevzemmobile.data.api.ApiClient

class HttpIdentityVerificationRepository(
    private val apiClient: ApiClient,
) : IdentityVerificationRepository {

    override suspend fun verifyAndRegister(
        selfieBytes: ByteArray,
        idBytes: ByteArray,
        variant: String,
    ): Result<String> = runCatching {
        val response = apiClient.client.post("${apiClient.baseUrl}/api/auth/citizen/verify-and-register-by-document") {
            // ML inference can take 20-40s; override the ApiClient default of 15s.
            timeout { requestTimeoutMillis = 90_000; socketTimeoutMillis = 90_000 }
            setBody(MultiPartFormDataContent(
                formData {
                    append("variant", variant)
                    append("selfieFrames", selfieBytes, Headers.build {
                        append(HttpHeaders.ContentType, "image/jpeg")
                        append(HttpHeaders.ContentDisposition, "filename=\"selfie.jpg\"")
                    })
                    append("idFront", idBytes, Headers.build {
                        append(HttpHeaders.ContentType, "image/jpeg")
                        append(HttpHeaders.ContentDisposition, "filename=\"id.jpg\"")
                    })
                }
            ))
        }
        when (response.status) {
            HttpStatusCode.OK -> response.body<RegisterByDocumentResponseDto>().code
            HttpStatusCode.UnprocessableEntity -> {
                val problem = response.body<ProblemDetailsDto>()
                throw DocumentVerificationException(
                    problem.detail?.split(", ") ?: emptyList()
                )
            }
            else -> throw Exception("Verification request failed: ${response.status}")
        }
    }
}
