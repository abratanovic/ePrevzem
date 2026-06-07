package si.mentis.eprevzemmobile.data.registration

import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.HttpStatusCode
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.api.DeviceSessionDto
import si.mentis.eprevzemmobile.data.api.OnboardingPreviewDto
import si.mentis.eprevzemmobile.data.api.RedeemRequestDto
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole

class HttpRegistrationRepository(
    private val api: ApiClient,
    private val sessionStore: DeviceSessionStore,
) : RegistrationRepository {

    override suspend fun validateCode(code: String): Result<String> {
        return try {
            val normalized = code.replace("-", "").uppercase()
            val response = api.client.get("${api.baseUrl}/api/onboarding/$normalized")
            if (response.status.value in 200..299) {
                Result.success(normalized)
            } else {
                Result.failure(InvalidCodeException())
            }
        } catch (e: Exception) {
            Result.failure(NetworkException())
        }
    }

    override suspend fun fetchAccountPreview(validatedCode: String): Result<AppUser> {
        return try {
            val response = api.client.get("${api.baseUrl}/api/onboarding/$validatedCode")
            if (response.status.value !in 200..299) {
                return Result.failure(InvalidCodeException())
            }
            val dto = response.body<OnboardingPreviewDto>()
            Result.success(dto.toAppUser(validatedCode))
        } catch (e: Exception) {
            Result.failure(NetworkException())
        }
    }

    override suspend fun confirmAccount(
        validatedCode: String,
        publicKey: String,
    ): Result<AppUser> {
        return try {
            val fingerprint = sessionStore.fingerprint()
            val request = RedeemRequestDto(
                publicKeyPem = publicKey,
                deviceFingerprint = fingerprint,
                label = null,
            )
            val response = api.client.post("${api.baseUrl}/api/onboarding/$validatedCode/redeem") {
                setBody(request)
            }
            if (response.status.value !in 200..299) {
                return Result.failure(InvalidCodeException())
            }
            val dto = response.body<DeviceSessionDto>()
            sessionStore.saveSession(
                deviceId = dto.deviceId,
                accessToken = dto.accessToken,
                accessExpiresAt = dto.accessTokenExpiresAt,
                refreshToken = dto.refreshToken,
            )
            Result.success(dto.toAppUser())
        } catch (e: Exception) {
            Result.failure(NetworkException())
        }
    }

    private fun OnboardingPreviewDto.toAppUser(code: String): AppUser {
        return if (kind == "Employee") {
            AppUser.Employee(
                id = code,
                fullName = "$firstName $lastName",
                email = email ?: "",
                phone = phoneNumber ?: "",
                status = "Aktiven",
                validUntil = "",
                organizationId = "",
                organizationName = organizationName ?: "",
                organizationType = "",
                organizationLocation = "",
                roles = roles.mapNotNull { runCatching { EmployeeRole.valueOf(it) }.getOrNull() },
                emso = emso,
            )
        } else {
            AppUser.RegularUser(
                id = code,
                fullName = "$firstName $lastName",
                email = email ?: "",
                phone = phoneNumber ?: "",
                emso = emso,
            )
        }
    }

    private fun DeviceSessionDto.toAppUser(): AppUser {
        return if (role == "Employee") {
            AppUser.Employee(
                id = deviceId,
                fullName = "$firstName $lastName",
                email = "",
                phone = "",
                status = "Aktiven",
                validUntil = "",
                organizationId = organizationId ?: "",
                organizationName = organizationName ?: "",
                organizationType = "",
                organizationLocation = "",
                roles = roles.mapNotNull { runCatching { EmployeeRole.valueOf(it) }.getOrNull() },
                emso = emso,
            )
        } else {
            AppUser.RegularUser(
                id = deviceId,
                fullName = "$firstName $lastName",
                email = "",
                phone = "",
                emso = emso,
            )
        }
    }
}
