package si.mentis.eprevzemmobile.data.identity

import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.request.forms.MultiPartFormDataContent
import io.ktor.client.request.forms.formData
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.Headers
import io.ktor.http.HttpHeaders
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.PlatformConfig
import si.mentis.eprevzemmobile.data.api.ApiClient

class HttpIdentityVerificationRepository(
    private val apiClient: ApiClient,
    private val cvIdentityBaseUrl: String = PlatformConfig.cvIdentityBaseUrl,
) : IdentityVerificationRepository {

    private val cvClient = HttpClient {
        install(ContentNegotiation) {
            json(Json { ignoreUnknownKeys = true; explicitNulls = false })
        }
        install(HttpTimeout) {
            requestTimeoutMillis = 60_000
            connectTimeoutMillis = 10_000
            socketTimeoutMillis = 60_000
        }
    }

    override suspend fun verify(
        selfieBytes: ByteArray,
        idBytes: ByteArray,
        variant: String,
    ): Result<VerifyResponseDto> = runCatching {
        val response = cvClient.post("$cvIdentityBaseUrl/verify") {
            setBody(MultiPartFormDataContent(
                formData {
                    append("variant", variant)
                    append("selfie_frames", selfieBytes, Headers.build {
                        append(HttpHeaders.ContentType, "image/jpeg")
                        append(HttpHeaders.ContentDisposition, "filename=\"selfie.jpg\"")
                    })
                    append("id_front", idBytes, Headers.build {
                        append(HttpHeaders.ContentType, "image/jpeg")
                        append(HttpHeaders.ContentDisposition, "filename=\"id.jpg\"")
                    })
                }
            ))
        }
        response.body<VerifyResponseDto>()
    }

    override suspend fun registerByDocument(
        emso: String,
        firstName: String,
        lastName: String,
    ): Result<String> = runCatching {
        val response = apiClient.client.post("${apiClient.baseUrl}/api/auth/citizen/register-by-document") {
            contentType(ContentType.Application.Json)
            setBody(RegisterByDocumentRequestDto(emso, firstName, lastName))
        }
        response.body<RegisterByDocumentResponseDto>().code
    }
}
