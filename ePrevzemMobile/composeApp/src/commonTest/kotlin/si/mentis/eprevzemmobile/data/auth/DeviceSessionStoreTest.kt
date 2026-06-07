package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class
DeviceSessionStoreTest {

    @Test
    fun saveSession_persists_session_under_account_namespace() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())

        store.saveSession(
            deviceId = "dev-123",
            accessToken = "access-token-abc",
            accessExpiresAt = "2026-06-07T12:00:00Z",
            refreshToken = "refresh-token-xyz",
        )

        assertEquals("dev-123", store.deviceId("dev-123"))
        assertEquals("access-token-abc", store.accessToken("dev-123"))
        assertEquals("refresh-token-xyz", store.refreshToken("dev-123"))
    }

    @Test
    fun two_accounts_keep_separate_sessions() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())
        store.saveSession("dev-A", "access-A", "2026-06-07T12:00:00Z", "refresh-A")
        store.saveSession("dev-B", "access-B", "2026-06-07T12:00:00Z", "refresh-B")

        assertEquals("access-A", store.accessToken("dev-A"))
        assertEquals("access-B", store.accessToken("dev-B"))
        assertEquals("dev-A", store.deviceId("dev-A"))
        assertEquals("dev-B", store.deviceId("dev-B"))
    }

    @Test
    fun updateTokens_overwrites_tokens_for_that_account_only() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())
        store.saveSession("dev-A", "old-A", "2026-06-07T10:00:00Z", "old-refresh-A")
        store.saveSession("dev-B", "access-B", "2026-06-07T10:00:00Z", "refresh-B")

        store.updateTokens("dev-A", "new-A", "2026-06-07T14:00:00Z", "new-refresh-A")

        assertEquals("dev-A", store.deviceId("dev-A"))
        assertEquals("new-A", store.accessToken("dev-A"))
        assertEquals("new-refresh-A", store.refreshToken("dev-A"))
        assertEquals("access-B", store.accessToken("dev-B"))
    }

    @Test
    fun clear_removes_only_that_account_and_preserves_fingerprint() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())
        store.saveSession("dev-A", "access-A", "2026-06-07T12:00:00Z", "refresh-A")
        store.saveSession("dev-B", "access-B", "2026-06-07T12:00:00Z", "refresh-B")
        val fp = store.fingerprint()

        store.clear("dev-A")

        assertNull(store.deviceId("dev-A"))
        assertNull(store.accessToken("dev-A"))
        assertEquals("access-B", store.accessToken("dev-B"))
        assertEquals(fp, store.fingerprint())
    }

    @Test
    fun fingerprint_is_stable_and_shared() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())
        val first = store.fingerprint()
        assertEquals(first, store.fingerprint())
    }

    @Test
    fun read_returns_null_when_account_not_set() = runTest {
        val store = DeviceSessionStore(FakeSessionStorage())
        assertNull(store.deviceId("missing"))
        assertNull(store.accessToken("missing"))
        assertNull(store.refreshToken("missing"))
    }
}
