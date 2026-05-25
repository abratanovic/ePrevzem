package si.mentis.eprevzemmobile.data.registration

import si.mentis.eprevzemmobile.domain.User

interface RegistrationRepository {
    suspend fun validateCode(code: String): Result<String>
    suspend fun fetchAccountPreview(validatedCode: String): Result<User>
    suspend fun confirmAccount(validatedCode: String, publicKey: String): Result<User>
}
