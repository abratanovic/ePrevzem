package si.mentis.eprevzemmobile.data.logevent

import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
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

class HttpLogEventRepositoryTest {

    private val singleEntryJson = """
        [{"id":"a1","occurredAt":"2026-05-12T14:32:00Z","actorKind":"Citizen",
          "actorCitizenUserId":"acc-1","action":"PackagePickedUpByCitizen",
          "targetKind":"Package","targetId":"p1",
          "details":{"documentTitle":"Diploma","organizationName":"UL",
                     "lockerLabel":"Paketnik #7","location":"Ljubljana"}}]
    """.trimIndent()

    @Test
    fun citizen_profile_calls_citizen_endpoint_with_bearer_and_maps_entries() = runTest {
        var capturedPath: String? = null
        var capturedAuth: String? = null
        val engine = MockEngine { request ->
            capturedPath = request.url.encodedPath
            capturedAuth = request.headers[HttpHeaders.Authorization]
            respond(
                content = singleEntryJson,
                status = HttpStatusCode.OK,
                headers = headersOf("Content-Type", "application/json"),
            )
        }
        val repository = buildRepository(engine, citizenProfile())

        val result = repository.getLogEventsForCurrentUser()

        assertEquals("/api/citizen/audit-log", capturedPath)
        assertEquals("Bearer AT-1", capturedAuth)
        assertEquals(1, result.size)
        assertEquals(LogAction.PackagePickedUpByCitizen, result[0].action)
        assertEquals(LogActorKind.Citizen, result[0].actorKind)
        assertEquals("Diploma", result[0].details?.documentTitle)
    }

    @Test
    fun operator_profile_calls_operator_endpoint() = runTest {
        var capturedPath: String? = null
        val engine = MockEngine { request ->
            capturedPath = request.url.encodedPath
            respond(
                content = "[]",
                status = HttpStatusCode.OK,
                headers = headersOf("Content-Type", "application/json"),
            )
        }
        val repository = buildRepository(engine, operatorProfile())

        repository.getLogEventsForCurrentUser()

        assertEquals("/api/operator/audit-log", capturedPath)
    }

    @Test
    fun server_error_returns_empty() = runTest {
        val engine = MockEngine { respond(content = "", status = HttpStatusCode.InternalServerError) }
        val repository = buildRepository(engine, citizenProfile())

        assertEquals(emptyList(), repository.getLogEventsForCurrentUser())
    }

    @Test
    fun network_error_returns_empty_without_throwing() = runTest {
        val engine = MockEngine { throw RuntimeException("connection refused") }
        val repository = buildRepository(engine, citizenProfile())

        assertEquals(emptyList(), repository.getLogEventsForCurrentUser())
    }

    private suspend fun buildRepository(
        engine: MockEngine,
        profile: suspend () -> AppUser?,
    ): HttpLogEventRepository {
        val sessionStore = DeviceSessionStore(FakeSessionStorage())
        sessionStore.saveSession("acc-1", "AT-1", "2026-06-07T10:15:00Z", "RT-1")
        val api = ApiClient(
            baseUrl = "http://localhost:8080",
            sessionStore = sessionStore,
            activeAccountId = { "acc-1" },
            httpClient = createMockHttpClient(engine),
        )
        return HttpLogEventRepository(api, profile)
    }

    private fun citizenProfile(): suspend () -> AppUser? = {
        AppUser.RegularUser(id = "acc-1", fullName = "Janez Novak", email = "", phone = "")
    }

    private fun operatorProfile(): suspend () -> AppUser? = {
        AppUser.Employee(
            id = "acc-1",
            fullName = "Ana Kovač",
            email = "",
            phone = "",
            status = "Aktiven",
            validUntil = "",
            organizationId = "org-1",
            organizationName = "",
            organizationType = "",
            organizationLocation = "",
            roles = listOf(EmployeeRole.Operator),
        )
    }

    private fun createMockHttpClient(engine: MockEngine): HttpClient =
        HttpClient(engine) {
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
