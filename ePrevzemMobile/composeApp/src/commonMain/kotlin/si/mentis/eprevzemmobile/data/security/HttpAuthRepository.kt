package si.mentis.eprevzemmobile.data.security

import io.ktor.client.call.body
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.ChallengeRequestDto
import si.mentis.eprevzemmobile.data.api.ChallengeResponseDto
import si.mentis.eprevzemmobile.data.api.VerifyRequestDto
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore

@OptIn(ExperimentalEncodingApi::class)
class HttpAuthRepository(
    private val api: ApiClient,
    private val sessionStore: DeviceSessionStore,
) : AuthRepository {

    override suspend fun getChallenge(deviceId: String): Result<ByteArray> {
        return try {
            val request = ChallengeRequestDto(deviceId)
            val response = api.client.post("${api.baseUrl}/api/auth/device/challenge") {
                setBody(request)
            }
            if (response.status.value !in 200..299) {
                return Result.failure(Exception("Failed to get challenge"))
            }
            val dto = response.body<ChallengeResponseDto>()
            val challengeBytes = Base64.decode(dto.challenge)
            Result.success(challengeBytes)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override suspend fun verifySignature(deviceId: String, signature: ByteArray): Result<String> {
        return try {
            val request = VerifyRequestDto(
                deviceId = deviceId,
                signature = Base64.encode(signature),
            )
            val response = api.client.post("${api.baseUrl}/api/auth/device/verify") {
                setBody(request)
            }
            if (response.status.value !in 200..299) {
                return Result.failure(Exception("Failed to verify signature"))
            }
            val dto = response.body<si.mentis.eprevzemmobile.data.api.DeviceSessionDto>()
            sessionStore.updateTokens(
                accountId = deviceId,
                accessToken = dto.accessToken,
                accessExpiresAt = dto.accessTokenExpiresAt,
                refreshToken = dto.refreshToken,
            )
            Result.success(dto.accessToken)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
