package si.mentis.eprevzemmobile.data.registration

import si.mentis.eprevzemmobile.domain.AppUser

interface RegistrationRepository {
    suspend fun validateCode(code: String): Result<String>
    suspend fun fetchAccountPreview(validatedCode: String): Result<AppUser>
    suspend fun confirmAccount(validatedCode: String, publicKey: String): Result<AppUser>
}
