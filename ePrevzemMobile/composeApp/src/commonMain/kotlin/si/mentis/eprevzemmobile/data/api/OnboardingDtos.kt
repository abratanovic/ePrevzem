package si.mentis.eprevzemmobile.data.api

import kotlinx.serialization.Serializable

@Serializable
data class OnboardingPreviewDto(
    val kind: String,
    val firstName: String,
    val lastName: String,
    val email: String? = null,
    val phoneNumber: String? = null,
    val organizationName: String? = null,
    val roles: List<String> = emptyList(),
    val expiresAt: String,
)

@Serializable
data class RedeemRequestDto(
    val publicKeyPem: String,
    val deviceFingerprint: String,
    val label: String? = null,
)

@Serializable
data class DeviceSessionDto(
    val role: String,
    val deviceId: String,
    val accessToken: String,
    val accessTokenExpiresAt: String,
    val refreshToken: String,
    val refreshTokenExpiresAt: String,
    val firstName: String,
    val lastName: String,
    val email: String? = null,
    val phoneNumber: String? = null,
    val organizationId: String? = null,
    val organizationName: String? = null,
    val roles: List<String> = emptyList(),
)

@Serializable
data class ChallengeRequestDto(
    val deviceId: String,
)

@Serializable
data class ChallengeResponseDto(
    val challenge: String,
    val expiresAt: String,
)

@Serializable
data class VerifyRequestDto(
    val deviceId: String,
    val signature: String,
)

@Serializable
data class RefreshRequestDto(
    val refreshToken: String,
)
