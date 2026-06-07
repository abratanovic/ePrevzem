package si.mentis.eprevzemmobile.data.api

import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.HttpRequestBuilder
import io.ktor.client.request.get
import io.ktor.client.request.header
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.http.ContentType
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.PlatformConfig
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore

class ApiClient(
    val baseUrl: String = PlatformConfig.eprevzemApiBaseUrl,
    private val sessionStore: DeviceSessionStore? = null,
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

    /**
     * Issues an authenticated GET against [path] (relative to [baseUrl]),
     * attaching the stored citizen access token as a bearer credential. On a
     * 401 the access token is refreshed once via the device refresh endpoint
     * and the request is retried with the new token; if refresh fails the
     * original 401 response is returned for the caller to handle (e.g. force
     * re-login). Without a [sessionStore] the request is sent unauthenticated.
     */
    suspend fun authorizedGet(path: String): HttpResponse {
        val url = "$baseUrl$path"
        val store = sessionStore
        var response = client.get(url) { bearer(store?.accessToken()) }
        if (response.status == HttpStatusCode.Unauthorized && store != null) {
            val refreshed = tryRefresh(store)
            if (refreshed != null) {
                response = client.get(url) { bearer(refreshed) }
            }
        }
        return response
    }

    private suspend fun tryRefresh(store: DeviceSessionStore): String? {
        val refreshToken = store.refreshToken() ?: return null
        return try {
            val response = client.post("$baseUrl/api/auth/device/refresh") {
                setBody(RefreshRequestDto(refreshToken))
            }
            if (response.status.value !in 200..299) {
                return null
            }
            val dto = response.body<DeviceSessionDto>()
            store.updateTokens(
                accessToken = dto.accessToken,
                accessExpiresAt = dto.accessTokenExpiresAt,
                refreshToken = dto.refreshToken,
            )
            dto.accessToken
        } catch (e: Exception) {
            null
        }
    }

    private fun HttpRequestBuilder.bearer(token: String?) {
        if (token != null) {
            header(HttpHeaders.Authorization, "Bearer $token")
        }
    }
}
