package si.mentis.eprevzemmobile.data.registration

import kotlinx.coroutines.delay
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole

private const val CODE_REGULAR_USER = "ABC123XYZ"
private const val CODE_OPERATOR = "GHI789RST"
private const val CODE_RECORD_MANAGER = "DEF456UVW"

private val FAKE_USERS: Map<String, AppUser> = mapOf(
    CODE_REGULAR_USER to AppUser.RegularUser(
        id = "user-$CODE_REGULAR_USER",
        fullName = "Marko Horvat",
        email = "marko.horvat@gmail.com",
        phone = "+386 41 234 567",
    ),
    CODE_OPERATOR to AppUser.Employee(
        id = "user-$CODE_OPERATOR",
        fullName = "Janez Kovač",
        email = "janez.kovac@gov.si",
        phone = "+386 40 111 222",
        status = "Aktiven",
        validUntil = "14. nov 2025",
        organizationId = "org-001",
        organizationName = "Upravna enota Ljubljana",
        organizationType = "Javna uprava",
        organizationLocation = "Adamič-Lundrovo nabrežje 2, Ljubljana",
        roles = listOf(EmployeeRole.Operator),
    ),
    CODE_RECORD_MANAGER to AppUser.Employee(
        id = "user-$CODE_RECORD_MANAGER",
        fullName = "Ana Novak",
        email = "ana.novak@gov.si",
        phone = "+386 51 987 654",
        status = "Aktiven",
        validUntil = "30. jun 2026",
        organizationId = "org-002",
        organizationName = "Ministrstvo za notranje zadeve",
        organizationType = "Državni organ",
        organizationLocation = "Štefanova ulica 2, Ljubljana",
        roles = listOf(EmployeeRole.RecordManager),
    ),
)

class FakeRegistrationRepository : RegistrationRepository {

    override suspend fun validateCode(code: String): Result<String> {
        delay(1200)
        val normalised = code.replace("-", "").uppercase()
        return if (FAKE_USERS.containsKey(normalised)) {
            Result.success(normalised)
        } else {
            Result.failure(InvalidCodeException())
        }
    }

    override suspend fun fetchAccountPreview(validatedCode: String): Result<AppUser> {
        delay(300)
        return FAKE_USERS[validatedCode]
            ?.let { Result.success(it) }
            ?: Result.failure(InvalidCodeException())
    }

    override suspend fun confirmAccount(
        validatedCode: String,
        publicKey: String,
    ): Result<AppUser> {
        delay(800)
        return FAKE_USERS[validatedCode]
            ?.let { Result.success(it) }
            ?: Result.failure(InvalidCodeException())
    }
}

class InvalidCodeException : Exception("Koda ni veljavna ali je potekla")
class NetworkException : Exception("Napaka pri povezavi s strežnikom")
