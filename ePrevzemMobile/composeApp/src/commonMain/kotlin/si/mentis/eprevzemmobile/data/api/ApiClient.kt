package si.mentis.eprevzemmobile.data.api

import io.ktor.client.HttpClient
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.http.ContentType
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.PlatformConfig

class ApiClient(
    val baseUrl: String = PlatformConfig.eprevzemApiBaseUrl,
    httpClient: HttpClient? = null,
) {
    val client: HttpClient = httpClient ?: HttpClient {
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
            contentType(ContentType.Application.Json)
        }
    }
}
