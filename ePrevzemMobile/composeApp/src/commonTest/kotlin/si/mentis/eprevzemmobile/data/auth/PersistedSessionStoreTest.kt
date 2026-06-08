package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.test.runTest
import kotlinx.serialization.builtins.ListSerializer
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs
import kotlin.test.assertNull
import kotlin.test.assertTrue

class PersistedSessionStoreTest {

    private val sampleRegularUser = AppUser.RegularUser(
        id = "u-1",
        fullName = "Marko Horvat",
        email = "m@example.com",
        phone = "+386 41 000 001",
    )

    private val sampleEmployee = AppUser.Employee(
        id = "u-2",
        fullName = "Ana Novak",
        email = "a@example.com",
        phone = "+386 41 000 002",
        status = "Aktiven",
        validUntil = "31. dec 2026",
        organizationId = "org-1",
        organizationName = "MNZ",
        organizationType = "Državni organ",
        organizationLocation = "Štefanova 2",
        roles = listOf(EmployeeRole.RecordManager),
    )

    @Test
    fun hydrate_with_empty_storage_leaves_unauthenticated_with_no_profiles() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()

        assertEquals(AuthSession.Unauthenticated, store.session.value)
        assertEquals(emptyList(), store.profiles.value)
        assertNull(store.activeProfile())
    }

    @Test
    fun addProfile_persists_and_updates_profiles_flow() = runTest {
        val storage = FakeSessionStorage()
        val store = PersistedSessionStore(storage)
        store.hydrate()

        store.addProfile(sampleRegularUser)

        assertEquals(listOf(sampleRegularUser), store.profiles.value)
        assertTrue(storage.snapshot().containsKey("auth.persisted_profiles"))
    }

    @Test
    fun addProfile_replaces_existing_profile_with_same_id() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()
        store.addProfile(sampleRegularUser)

        val updated = sampleRegularUser.copy(fullName = "Marko Updated")
        store.addProfile(updated)

        assertEquals(listOf(updated), store.profiles.value)
    }

    @Test
    fun setAuthenticated_with_known_id_emits_authenticated_and_records_active_id() = runTest {
        val storage = FakeSessionStorage()
        val store = PersistedSessionStore(storage)
        store.hydrate()
        store.addProfile(sampleRegularUser)
        store.addProfile(sampleEmployee)

        store.setAuthenticated(sampleEmployee.id)

        val current = store.session.value
        assertIs<AuthSession.Authenticated>(current)
        assertEquals(sampleEmployee, current.user)
        assertEquals(sampleEmployee, store.activeProfile())
        assertEquals(sampleEmployee.id, storage.snapshot()["auth.active_profile_id"])
    }

    @Test
    fun setAuthenticated_with_unknown_id_throws() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()
        store.addProfile(sampleRegularUser)

        try {
            store.setAuthenticated("does-not-exist")
            error("expected IllegalArgumentException")
        } catch (e: IllegalArgumentException) {
            // expected
        }
    }

    @Test
    fun switchProfile_changes_active_without_clearing_session() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()
        store.addProfile(sampleRegularUser)
        store.addProfile(sampleEmployee)
        store.setAuthenticated(sampleRegularUser.id)

        store.switchProfile(sampleEmployee.id)

        val current = store.session.value
        assertIs<AuthSession.Authenticated>(current)
        assertEquals(sampleEmployee, current.user)
    }

    @Test
    fun removeProfile_drops_the_profile_and_signs_out_if_it_was_active() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()
        store.addProfile(sampleRegularUser)
        store.addProfile(sampleEmployee)
        store.setAuthenticated(sampleEmployee.id)

        store.removeProfile(sampleEmployee.id)

        assertEquals(listOf(sampleRegularUser), store.profiles.value)
        assertEquals(AuthSession.Unauthenticated, store.session.value)
        assertNull(store.activeProfile())
    }

    @Test
    fun removeProfile_of_non_active_keeps_session_intact() = runTest {
        val store = PersistedSessionStore(FakeSessionStorage())
        store.hydrate()
        store.addProfile(sampleRegularUser)
        store.addProfile(sampleEmployee)
        store.setAuthenticated(sampleEmployee.id)

        store.removeProfile(sampleRegularUser.id)

        assertEquals(listOf(sampleEmployee), store.profiles.value)
        val current = store.session.value
        assertIs<AuthSession.Authenticated>(current)
        assertEquals(sampleEmployee, current.user)
    }

    @Test
    fun clear_signs_out_but_preserves_profiles_and_active_id() = runTest {
        val storage = FakeSessionStorage()
        val store = PersistedSessionStore(storage)
        store.hydrate()
        store.addProfile(sampleEmployee)
        store.setAuthenticated(sampleEmployee.id)

        store.clear()

        assertEquals(AuthSession.Unauthenticated, store.session.value)
        assertEquals(listOf(sampleEmployee), store.profiles.value)
        assertEquals(sampleEmployee.id, storage.snapshot()["auth.active_profile_id"])
    }

    @Test
    fun forgetAllIdentities_wipes_profiles_and_active_id() = runTest {
        val storage = FakeSessionStorage()
        val store = PersistedSessionStore(storage)
        store.hydrate()
        store.addProfile(sampleRegularUser)
        store.addProfile(sampleEmployee)
        store.setAuthenticated(sampleEmployee.id)

        store.forgetAllIdentities()

        assertEquals(AuthSession.Unauthenticated, store.session.value)
        assertEquals(emptyList(), store.profiles.value)
        val keys = storage.snapshot().keys
        assertTrue("auth.persisted_profiles" !in keys)
        assertTrue("auth.active_profile_id" !in keys)
    }

    @Test
    fun hydrate_loads_existing_profiles_and_active_id() = runTest {
        val json = Json { ignoreUnknownKeys = true }
        val storage = FakeSessionStorage(
            mapOf(
                "auth.persisted_profiles" to json.encodeToString(
                    ListSerializer(AppUser.serializer()),
                    listOf(sampleRegularUser, sampleEmployee),
                ),
                "auth.active_profile_id" to sampleEmployee.id,
            ),
        )
        val store = PersistedSessionStore(storage, json)

        store.hydrate()

        assertEquals(listOf(sampleRegularUser, sampleEmployee), store.profiles.value)
        assertEquals(sampleEmployee, store.activeProfile())
        assertEquals(AuthSession.Unauthenticated, store.session.value)
    }

    @Test
    fun hydrate_migrates_legacy_single_user_payload() = runTest {
        val json = Json { ignoreUnknownKeys = true }
        val storage = FakeSessionStorage(
            mapOf(
                "auth.persisted_user" to json.encodeToString(AppUser.serializer(), sampleEmployee),
            ),
        )
        val store = PersistedSessionStore(storage, json)

        store.hydrate()

        assertEquals(listOf(sampleEmployee), store.profiles.value)
        assertEquals(sampleEmployee, store.activeProfile())
        val snapshot = storage.snapshot()
        assertTrue("auth.persisted_profiles" in snapshot)
        assertEquals(sampleEmployee.id, snapshot["auth.active_profile_id"])
        assertTrue("auth.persisted_user" !in snapshot)
    }

    @Test
    fun hydrate_does_not_overwrite_profiles_when_legacy_key_coexists() = runTest {
        val json = Json { ignoreUnknownKeys = true }
        val storage = FakeSessionStorage(
            mapOf(
                "auth.persisted_profiles" to json.encodeToString(
                    ListSerializer(AppUser.serializer()),
                    listOf(sampleRegularUser, sampleEmployee),
                ),
                "auth.active_profile_id" to sampleRegularUser.id,
                "auth.persisted_user" to json.encodeToString(AppUser.serializer(), sampleEmployee),
            ),
        )
        val store = PersistedSessionStore(storage, json)

        store.hydrate()

        assertEquals(listOf(sampleRegularUser, sampleEmployee), store.profiles.value)
        assertEquals(sampleRegularUser, store.activeProfile())
        assertTrue("auth.persisted_user" !in storage.snapshot())
    }

    @Test
    fun hydrate_with_invalid_profiles_payload_clears_dangling_active_id() = runTest {
        val storage = FakeSessionStorage(
            mapOf(
                "auth.persisted_profiles" to "{not-json",
                "auth.active_profile_id" to sampleEmployee.id,
            ),
        )
        val store = PersistedSessionStore(storage)

        store.hydrate()

        assertEquals(emptyList(), store.profiles.value)
        assertNull(store.activeProfile())
        val snapshot = storage.snapshot()
        assertTrue("auth.persisted_profiles" !in snapshot)
        assertTrue("auth.active_profile_id" !in snapshot)
    }
}
