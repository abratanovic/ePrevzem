package si.mentis.eprevzemmobile.data.registration

import kotlinx.coroutines.delay
import si.mentis.eprevzemmobile.domain.User

private const val VALID_CODE = "ABC123XYZ"

class FakeRegistrationRepository : RegistrationRepository {

    override suspend fun validateCode(code: String): Result<String> {
        delay(1200)
        val normalised = code.replace("-", "").uppercase()
        return if (normalised == VALID_CODE) {
            Result.success(normalised)
        } else {
            Result.failure(InvalidCodeException())
        }
    }

    override suspend fun fetchAccountPreview(validatedCode: String): Result<User> {
        delay(300)
        return Result.success(fakeUser(validatedCode))
    }

    override suspend fun confirmAccount(
        validatedCode: String,
        pin: String,
        biometricEnabled: Boolean,
    ): Result<User> {
        delay(800)
        return Result.success(fakeUser(validatedCode, biometricEnabled))
    }

    private fun fakeUser(code: String, biometricEnabled: Boolean = true) = User(
        id = "user-$code",
        fullName = "Marko Horvat",
        email = "marko.horvat@gov.si",
        phone = "+386 41 234 567",
        status = "Aktiven",
        validUntil = "14. nov 2025",
        organizationName = "Upravna enota Ljubljana",
        organizationType = "Javna uprava",
        organizationLocation = "Adamič-Lundrovo nabrežje 2, Ljubljana",
        isBiometricEnabled = biometricEnabled,
    )
}

class InvalidCodeException : Exception("Koda ni veljavna ali je potekla")
