package si.mentis.eprevzemmobile.data.registration

import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.request.get
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.http.HttpMethod
import io.ktor.http.headersOf
import io.ktor.serialization.kotlinx.json.json
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore
import si.mentis.eprevzemmobile.data.auth.FakeSessionStorage
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs
import kotlin.test.assertTrue

class HttpRegistrationRepositoryTest {

    @Test
    fun validateCode_success_returns_normalized_code() = runTest {
        val mockEngine = MockEngine { request ->
            respond(
                content = """{"kind":"RegularUser","firstName":"John","lastName":"Doe","email":"john@example.com","phoneNumber":"+1234567890","organizationName":null,"roles":[],"expiresAt":"2026-12-31"}""",
                status = HttpStatusCode.OK,
                headers = headersOf("Content-Type", "application/json"),
            )
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.validateCode("ABC-123-XYZ")

        assertTrue(result.isSuccess)
        assertEquals("ABC123XYZ", result.getOrNull())
    }

    @Test
    fun validateCode_404_returns_failure() = runTest {
        val mockEngine = MockEngine { request ->
            respond(content = "", status = HttpStatusCode.NotFound)
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.validateCode("INVALID")

        assertTrue(result.isFailure)
        assertIs<InvalidCodeException>(result.exceptionOrNull())
    }

    @Test
    fun validateCode_410_returns_failure() = runTest {
        val mockEngine = MockEngine { request ->
            respond(content = "", status = HttpStatusCode.Gone)
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.validateCode("EXPIRED")

        assertTrue(result.isFailure)
        assertIs<InvalidCodeException>(result.exceptionOrNull())
    }

    @Test
    fun fetchAccountPreview_citizen_returns_regular_user() = runTest {
        val mockEngine = MockEngine { request ->
            // The mockEngine will respond to all requests with the same response
            respond(
                content = """{"kind":"RegularUser","firstName":"John","lastName":"Doe","email":"john@example.com","phoneNumber":"+386123456","emso":"0101000500001","roles":[],"expiresAt":"2026-12-31"}""",
                status = HttpStatusCode.OK,
                headers = headersOf("Content-Type", "application/json"),
            )
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.fetchAccountPreview("ABC123")

        assertTrue(result.isSuccess)
        val user = result.getOrNull()
        assertIs<AppUser.RegularUser>(user)
        assertEquals("John Doe", user.fullName)
        assertEquals("john@example.com", user.email)
        assertEquals("0101000500001", user.emso)
        assertEquals("+386123456", user.phone)
    }

    @Test
    fun fetchAccountPreview_employee_returns_employee_user_with_roles() = runTest {
        val mockEngine = MockEngine { request ->
            respond(
                content = """{"kind":"Employee","firstName":"Jane","lastName":"Smith","email":"jane@gov.si","phoneNumber":"+386987654","organizationName":"Ministry","roles":["Operator","RecordManager"],"expiresAt":"2026-12-31"}""",
                status = HttpStatusCode.OK,
                headers = headersOf("Content-Type", "application/json"),
            )
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.fetchAccountPreview("DEF456")

        assertTrue(result.isSuccess)
        val user = result.getOrNull()
        assertIs<AppUser.Employee>(user)
        assertEquals("Jane Smith", user.fullName)
        assertEquals("jane@gov.si", user.email)
        assertEquals("+386987654", user.phone)
        assertEquals("Ministry", user.organizationName)
        assertEquals(listOf(EmployeeRole.Operator, EmployeeRole.RecordManager), user.roles)
    }

    @Test
    fun fetchAccountPreview_404_returns_failure() = runTest {
        val mockEngine = MockEngine { request ->
            respond(content = "", status = HttpStatusCode.NotFound)
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.fetchAccountPreview("INVALID")

        assertTrue(result.isFailure)
        assertIs<InvalidCodeException>(result.exceptionOrNull())
    }


    @Test
    fun confirmAccount_failure_returns_invalid_code_exception() = runTest {
        val mockEngine = MockEngine { request ->
            respond(content = "", status = HttpStatusCode.BadRequest)
        }
        val mockClient = createMockHttpClient(mockEngine)
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        val repository = HttpRegistrationRepository(createApiClient(mockClient), sessionStore)

        val result = repository.confirmAccount("INVALID", "invalid-key")

        assertTrue(result.isFailure)
        assertIs<InvalidCodeException>(result.exceptionOrNull())
    }

    private fun createMockHttpClient(engine: MockEngine): HttpClient {
        return HttpClient(engine) {
            install(ContentNegotiation) {
                json(
                    Json {
                        ignoreUnknownKeys = true
                        explicitNulls = false
                    }
                )
            }
        }
    }

    private fun createApiClient(mockClient: HttpClient): ApiClient {
        return ApiClient(baseUrl = "http://localhost:5080", httpClient = mockClient)
    }
}
