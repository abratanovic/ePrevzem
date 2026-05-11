package si.mentis.eprevzemmobile.data.locker

import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json
import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import si.mentis.eprevzemmobile.data.locker.dto.OpenBoxRequest
import si.mentis.eprevzemmobile.data.locker.dto.OpenBoxResponse

class Direct4MeLockerRepository(
    private val baseUrl: String = "https://api-d4me-stage.direct4.me/sandbox/v1",
    private val apiKey: String = "",
    httpClient: HttpClient? = null,
) : LockerRepository {

    private val client: HttpClient = httpClient ?: HttpClient {
        install(ContentNegotiation) {
            json(
                Json {
                    ignoreUnknownKeys = true
                    explicitNulls = false
                }
            )
        }
        install(HttpTimeout) {
            requestTimeoutMillis = 15_000
            connectTimeoutMillis = 10_000
            socketTimeoutMillis = 15_000
        }
        defaultRequest {
            if (apiKey.isNotBlank()) {
                headers.append("Authorization", "Bearer $apiKey")
            }
        }
    }

    @OptIn(ExperimentalEncodingApi::class)
    override suspend fun openBox(boxId: Long, tokenFormat: Int): OpenBoxResult {
        val response: OpenBoxResponse = try {
            client.post("$baseUrl/Access/openbox") {
                contentType(ContentType.Application.Json)
                setBody(OpenBoxRequest(boxId = boxId, tokenFormat = tokenFormat))
            }.body()
        } catch (t: Throwable) {
            return OpenBoxResult.NetworkFailure(t)
        }

        if (response.result != 0 || response.errorNumber != 0 || response.data.isNullOrBlank()) {
            return OpenBoxResult.ApiFailure(response.errorNumber)
        }

        val decoded = try {
            Base64.decode(response.data)
        } catch (t: Throwable) {
            return OpenBoxResult.NetworkFailure(t)
        }

        val wavBytes = try {
            if (isGzipped(decoded)) gunzip(decoded) else decoded
        } catch (t: Throwable) {
            return OpenBoxResult.NetworkFailure(t)
        }

        return OpenBoxResult.Success(wavBytes)
    }
}
