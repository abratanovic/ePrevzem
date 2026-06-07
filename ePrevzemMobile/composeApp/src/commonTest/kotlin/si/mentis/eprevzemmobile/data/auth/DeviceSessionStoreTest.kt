package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class DeviceSessionStoreTest {

    @Test
    fun saveSession_persists_all_four_keys() = runTest {
        val storage = FakeSessionStorage()
        val store = DeviceSessionStore(storage)

        store.saveSession(
            deviceId = "dev-123",
            accessToken = "access-token-abc",
            accessExpiresAt = "2026-06-07T12:00:00Z",
            refreshToken = "refresh-token-xyz",
        )

        assertEquals("dev-123", store.deviceId())
        assertEquals("access-token-abc", store.accessToken())
        assertEquals("refresh-token-xyz", store.refreshToken())
    }

    @Test
    fun updateTokens_overwrites_token_keys_but_preserves_deviceId() = runTest {
        val storage = FakeSessionStorage()
        val store = DeviceSessionStore(storage)
        store.saveSession(
            deviceId = "dev-123",
            accessToken = "old-access",
            accessExpiresAt = "2026-06-07T10:00:00Z",
            refreshToken = "old-refresh",
        )

        store.updateTokens(
            accessToken = "new-access",
            accessExpiresAt = "2026-06-07T14:00:00Z",
            refreshToken = "new-refresh",
        )

        assertEquals("dev-123", store.deviceId())
        assertEquals("new-access", store.accessToken())
        assertEquals("new-refresh", store.refreshToken())
    }

    @Test
    fun clear_removes_all_keys() = runTest {
        val storage = FakeSessionStorage()
        val store = DeviceSessionStore(storage)
        store.saveSession(
            deviceId = "dev-123",
            accessToken = "access-token-abc",
            accessExpiresAt = "2026-06-07T12:00:00Z",
            refreshToken = "refresh-token-xyz",
        )

        store.clear()

        assertNull(store.deviceId())
        assertNull(store.accessToken())
        assertNull(store.refreshToken())
    }

    @Test
    fun read_returns_null_when_key_not_set() = runTest {
        val storage = FakeSessionStorage()
        val store = DeviceSessionStore(storage)

        assertNull(store.deviceId())
        assertNull(store.accessToken())
        assertNull(store.refreshToken())
    }
}
