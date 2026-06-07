# Operator Routing + Multi-Profile Session — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Branch authenticated routing in `App.kt` so `AppUser.Employee` lands on a new `OperatorHomeRoute` stub (the existing `ActivePickupsRoute` still serves `AppUser.RegularUser`), and refactor `SessionStore` to persist multiple profiles with a switchable active profile.

**Architecture:**
- New stub feature module `feature/operator/` following the existing `state + event + screen + route` pattern.
- `SessionStore` interface gains a `profiles: StateFlow<List<AppUser>>` and `addProfile / switchProfile / removeProfile / setAuthenticated(userId) / activeProfile()` operations. The single-user `persistedUser()` and `forgetIdentity()` go away.
- `PersistedSessionStore` now stores `List<AppUser>` under `auth.persisted_profiles` and the active id under `auth.active_profile_id`. A one-time migration reads the legacy `auth.persisted_user` key on first run.
- For testability, persistence is fronted by a small `SessionStorage` interface (production impl wraps the existing `SecureStorage`; tests use an in-memory fake).

**Tech Stack:** Kotlin Multiplatform, Compose Multiplatform, kotlinx.coroutines, kotlinx.serialization. Tests in `commonTest` with `kotlin-test` (already wired) + `kotlinx-coroutines-test` (to be added).

**Out of scope (subsequent plans):**
- Paketnik-scoped context screen and operator pickup repository (Phase 3 of the spec)
- Insert / remove flows (Phase 4)
- Profile tab UI with dropdown + audit log additions (Phase 5)
- Real QR scanner — the Operator home's button stays a no-op stub in this plan

---

### Task 1: OperatorHome stub screen files

**Files:**
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/operator/OperatorHomeState.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/operator/OperatorHomeEvent.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/operator/OperatorHomeScreen.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/operator/OperatorHomeRoute.kt`

- [ ] **Step 1: Create `OperatorHomeState.kt`**

```kotlin
package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Immutable

@Immutable
data class OperatorHomeState(
    val userName: String,
    val activeTab: OperatorTab = OperatorTab.Pickups,
)

enum class OperatorTab { Pickups, History, Profile }
```

- [ ] **Step 2: Create `OperatorHomeEvent.kt`**

```kotlin
package si.mentis.eprevzemmobile.feature.operator

sealed interface OperatorHomeEvent {
    data object ScanQrClicked : OperatorHomeEvent
    data class TabSelected(val tab: OperatorTab) : OperatorHomeEvent
}
```

- [ ] **Step 3: Create `OperatorHomeScreen.kt`**

```kotlin
package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun OperatorHomeScreen(
    state: OperatorHomeState,
    onEvent: (OperatorHomeEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    val navItems = listOf(
        EBottomNavItem(id = OperatorTab.Pickups.name, icon = EPrevzemIcons.home(), label = "Prevzemi"),
        EBottomNavItem(id = OperatorTab.History.name, icon = EPrevzemIcons.history(), label = "Zgodovina"),
        EBottomNavItem(id = OperatorTab.Profile.name, icon = EPrevzemIcons.profile(), label = "Profil"),
    )

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Home,
                leadingIcon = EPrevzemIcons.organization(),
                userInitials = state.userName.split(" ")
                    .mapNotNull { it.firstOrNull()?.toString() }
                    .take(2)
                    .joinToString(""),
                actionIcon = null,
            )
        },
        bottomBar = {
            EBottomNavigationBar(
                items = navItems,
                activeId = state.activeTab.name,
                onSelect = { id ->
                    val tab = OperatorTab.entries.firstOrNull { it.name == id } ?: return@EBottomNavigationBar
                    onEvent(OperatorHomeEvent.TabSelected(tab))
                },
            )
        },
    ) { _ ->
        EScreen {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(text = "DOBRODOŠLI", style = typo.caption, color = colors.textMuted)
                Text(
                    text = "Pozdravljeni, ${state.userName}",
                    style = typo.display.copy(fontSize = 28.sp),
                    color = colors.textPrimary,
                )
            }
            Spacer(Modifier.height(24.dp))
            EPrimaryButton(
                text = "Skeniraj QR kodo na paketniku",
                onClick = { onEvent(OperatorHomeEvent.ScanQrClicked) },
                modifier = Modifier.fillMaxWidth(),
            )
        }
    }
}
```

> **Note:** This step assumes `EPrimaryButton` lives in `core/designsystem/components/buttons/`. Verify the import path with `Glob` on `EPrimaryButton.kt` before pasting if your build fails — adjust the import if it's nested differently. The same goes for any token property (`typo.display`, `typo.caption`) — open `EPrevzemThemeTokens.kt` to confirm the exact names.

- [ ] **Step 4: Create `OperatorHomeRoute.kt`**

```kotlin
package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.domain.AppUser

@Composable
fun OperatorHomeRoute(
    user: AppUser.Employee,
    onScanQrClicked: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(OperatorHomeState(userName = user.fullName)) }

    OperatorHomeScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                OperatorHomeEvent.ScanQrClicked -> onScanQrClicked()
                is OperatorHomeEvent.TabSelected -> state = state.copy(activeTab = event.tab)
            }
        },
    )
}
```

- [ ] **Step 5: Verify it compiles**

Run: `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 6: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/operator/
git commit -m "feat(operator): add operator home stub screen"
```

---

### Task 2: Branch authenticated routing by user type in `App.kt`

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/App.kt`

- [ ] **Step 1: Add `OperatorHome` destination to the `AppDestination` sealed interface**

Edit `App.kt` — find the `private sealed interface AppDestination { ... }` block (near the top of the file) and add a new entry:

```kotlin
data object OperatorHome : AppDestination
```

Also extend the `AppDestination.depth` extension function — add a line for `OperatorHome` with the same depth as `ActivePickups` (3) so the slide-transition animation behaves consistently:

```kotlin
AppDestination.OperatorHome -> 3
```

- [ ] **Step 2: Branch the post-login destination based on user type**

Find the `LaunchedEffect(session) { destination = when (session) { ... } }` block in `App()`. Replace the `is AuthSession.Authenticated ->` arm with:

```kotlin
is AuthSession.Authenticated -> {
    val authedSession = session as AuthSession.Authenticated
    val target = when (authedSession.user) {
        is AppUser.RegularUser -> AppDestination.ActivePickups
        is AppUser.Employee    -> AppDestination.OperatorHome
    }
    when (destination) {
        AppDestination.Loading,
        AppDestination.Welcome,
        AppDestination.Login,
        AppDestination.RegistrationCode,
        is AppDestination.ConfirmAccount -> target
        else -> destination
    }
}
```

Add this import to `App.kt`:

```kotlin
import si.mentis.eprevzemmobile.domain.AppUser
```

- [ ] **Step 3: Render `OperatorHomeRoute` in the navigation `when`**

Find the `when (dest) { ... }` block inside the `AnimatedContent`. Add a branch:

```kotlin
AppDestination.OperatorHome -> {
    val authenticated = session as? AuthSession.Authenticated
    val employee = authenticated?.user as? AppUser.Employee
    if (employee != null) {
        OperatorHomeRoute(
            user = employee,
            onScanQrClicked = {
                // TODO(operator-paketnik-plan): navigate to OperatorScan
            },
        )
    }
}
```

Add the import for `OperatorHomeRoute`:

```kotlin
import si.mentis.eprevzemmobile.feature.operator.OperatorHomeRoute
```

- [ ] **Step 4: Verify Android assemble**

Run: `./gradlew :composeApp:assembleDebug`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 5: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/App.kt
git commit -m "feat(operator): route employees to operator home"
```

---

### Task 3: Add `kotlinx-coroutines-test` dependency

**Files:**
- Modify: `gradle/libs.versions.toml`
- Modify: `composeApp/build.gradle.kts`

- [ ] **Step 1: Add coroutines-test library to the version catalog**

Edit `gradle/libs.versions.toml`. In the `[libraries]` block, just below the `kotlinx-coroutines-core = ...` line, add:

```toml
kotlinx-coroutines-test = { module = "org.jetbrains.kotlinx:kotlinx-coroutines-test", version.ref = "kotlinx-coroutines" }
```

(No `[versions]` change needed — it reuses the existing `kotlinx-coroutines` version pin.)

- [ ] **Step 2: Wire it into `commonTest`**

Edit `composeApp/build.gradle.kts`. Find the `commonTest.dependencies { ... }` block (around line 72) and add the new dependency:

```kotlin
commonTest.dependencies {
    implementation(libs.kotlin.test)
    implementation(libs.kotlinx.coroutines.test)
}
```

- [ ] **Step 3: Verify the build still works**

Run: `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 4: Commit**

```bash
git add gradle/libs.versions.toml composeApp/build.gradle.kts
git commit -m "build: add kotlinx-coroutines-test to commonTest"
```

---

### Task 4: Extract `SessionStorage` abstraction

This task introduces a small interface so the next task's tests can fake persistence without touching the `expect class SecureStorage`. Behavior is unchanged.

**Files:**
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/SessionStorage.kt`
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt`
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/AppContainer.kt`

- [ ] **Step 1: Create the `SessionStorage` interface + production impl**

```kotlin
package si.mentis.eprevzemmobile.data.auth

import si.mentis.eprevzemmobile.data.security.SecureStorage

interface SessionStorage {
    suspend fun read(key: String): String?
    suspend fun write(key: String, value: String)
    suspend fun remove(key: String)
}

class SecureSessionStorage(
    private val secureStorage: SecureStorage = SecureStorage(),
) : SessionStorage {
    override suspend fun read(key: String): String? = secureStorage.readString(key)
    override suspend fun write(key: String, value: String) = secureStorage.writeString(key, value)
    override suspend fun remove(key: String) = secureStorage.remove(key)
}
```

- [ ] **Step 2: Change `PersistedSessionStore` to depend on `SessionStorage`**

Open `PersistedSessionStore.kt`. Replace the constructor and the body methods that read/write storage:

```kotlin
class PersistedSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
    private val json: Json = DefaultJson,
) : SessionStore {

    private val _session = MutableStateFlow<AuthSession>(AuthSession.Unknown)
    override val session: StateFlow<AuthSession> = _session.asStateFlow()

    override suspend fun hydrate() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun setAuthenticated(user: AppUser) {
        storage.write(KEY_PERSISTED_USER, json.encodeToString(AppUser.serializer(), user))
        _session.value = AuthSession.Authenticated(user)
    }

    override suspend fun clear() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun forgetIdentity() {
        storage.remove(KEY_PERSISTED_USER)
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun persistedUser(): AppUser? {
        val raw = storage.read(KEY_PERSISTED_USER) ?: return null
        return runCatching { json.decodeFromString(AppUser.serializer(), raw) }.getOrNull()
    }

    private companion object {
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
```

Delete the now-unused `SecureStorage` import in this file.

- [ ] **Step 3: AppContainer keeps using the default constructor**

`AppContainer.kt`'s line `val sessionStore: SessionStore = PersistedSessionStore()` continues to work because the new constructor defaults `storage = SecureSessionStorage()`. No change needed — but verify by reading the file.

- [ ] **Step 4: Verify it compiles**

Run: `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 5: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/SessionStorage.kt \
        composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt
git commit -m "refactor(auth): extract SessionStorage abstraction"
```

---

### Task 5: Define multi-profile `SessionStore` interface + write tests (RED)

This is the first half of a red-green pair. We define the new API and write the tests; they will fail to pass until Task 6 lands.

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/SessionStore.kt`
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt` (stubs only)
- Create: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/FakeSessionStorage.kt`
- Create: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStoreTest.kt`

- [ ] **Step 1: Rewrite `SessionStore` interface**

Replace the body of `SessionStore.kt` with:

```kotlin
package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.StateFlow
import si.mentis.eprevzemmobile.domain.AppUser

interface SessionStore {
    val session: StateFlow<AuthSession>
    val profiles: StateFlow<List<AppUser>>
    suspend fun hydrate()
    suspend fun addProfile(user: AppUser)
    suspend fun switchProfile(userId: String)
    suspend fun removeProfile(userId: String)
    suspend fun setAuthenticated(userId: String)
    suspend fun clear()
    suspend fun forgetAllIdentities()
    suspend fun activeProfile(): AppUser?
}
```

- [ ] **Step 2: Replace `PersistedSessionStore` body with `TODO()` stubs**

This makes the project compile so tests can be written against the new shape.

Open `PersistedSessionStore.kt`. Replace its body entirely with:

```kotlin
package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.domain.AppUser

class PersistedSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
    private val json: Json = DefaultJson,
) : SessionStore {

    private val _session = MutableStateFlow<AuthSession>(AuthSession.Unknown)
    override val session: StateFlow<AuthSession> = _session.asStateFlow()

    private val _profiles = MutableStateFlow<List<AppUser>>(emptyList())
    override val profiles: StateFlow<List<AppUser>> = _profiles.asStateFlow()

    override suspend fun hydrate() {
        TODO("implemented in Task 6")
    }
    override suspend fun addProfile(user: AppUser) {
        TODO("implemented in Task 6")
    }
    override suspend fun switchProfile(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun removeProfile(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun setAuthenticated(userId: String) {
        TODO("implemented in Task 6")
    }
    override suspend fun clear() {
        TODO("implemented in Task 6")
    }
    override suspend fun forgetAllIdentities() {
        TODO("implemented in Task 6")
    }
    override suspend fun activeProfile(): AppUser? {
        TODO("implemented in Task 6")
    }

    internal companion object {
        const val KEY_PROFILES = "auth.persisted_profiles"
        const val KEY_ACTIVE_PROFILE_ID = "auth.active_profile_id"
        const val LEGACY_KEY_PERSISTED_USER = "auth.persisted_user"
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
```

- [ ] **Step 3: Comment out / remove dependent call sites so the project compiles**

The old API used `persistedUser()`, `forgetIdentity()`, `setAuthenticated(user: AppUser)`. These no longer exist. Two callers need temporary edits — they'll be properly fixed in Tasks 7 and 8.

In `LoginRoute.kt`, replace the body of `finishAuthenticated()` with a temporary no-op so the file compiles:

```kotlin
suspend fun finishAuthenticated() {
    // Wired up in Task 8.
}
```

Also remove the `securityRepository.reset()` + `onResetSecureStorage()` else branch from `finishAuthenticated()` — leave the function as a placeholder.

Inside the `LoginEvent.ResetSecureStorageClicked ->` branch, change `sessionStore.forgetIdentity()` to `sessionStore.forgetAllIdentities()`.

In `ConfirmAccountRoute.kt`, change the line `sessionStore.setAuthenticated(user)` to:

```kotlin
sessionStore.addProfile(user)
sessionStore.setAuthenticated(user.id)
```

- [ ] **Step 4: Create the `FakeSessionStorage` test helper**

```kotlin
package si.mentis.eprevzemmobile.data.auth

class FakeSessionStorage(initial: Map<String, String> = emptyMap()) : SessionStorage {
    private val data = initial.toMutableMap()

    fun snapshot(): Map<String, String> = data.toMap()
    fun seed(key: String, value: String) { data[key] = value }

    override suspend fun read(key: String): String? = data[key]
    override suspend fun write(key: String, value: String) { data[key] = value }
    override suspend fun remove(key: String) { data.remove(key) }
}
```

- [ ] **Step 5: Write the failing tests**

```kotlin
package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import si.mentis.eprevzemmobile.domain.AppUser
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
        roles = listOf(si.mentis.eprevzemmobile.domain.EmployeeRole.RecordManager),
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
                    kotlinx.serialization.builtins.ListSerializer(AppUser.serializer()),
                    listOf(sampleRegularUser, sampleEmployee),
                ),
                "auth.active_profile_id" to sampleEmployee.id,
            )
        )
        val store = PersistedSessionStore(storage, json)

        store.hydrate()

        assertEquals(listOf(sampleRegularUser, sampleEmployee), store.profiles.value)
        assertEquals(sampleEmployee, store.activeProfile())
        // Active session remains Unauthenticated until biometric/PIN succeeds.
        assertEquals(AuthSession.Unauthenticated, store.session.value)
    }

    @Test
    fun hydrate_migrates_legacy_single_user_payload() = runTest {
        val json = Json { ignoreUnknownKeys = true }
        val storage = FakeSessionStorage(
            mapOf(
                "auth.persisted_user" to json.encodeToString(AppUser.serializer(), sampleEmployee),
            )
        )
        val store = PersistedSessionStore(storage, json)

        store.hydrate()

        assertEquals(listOf(sampleEmployee), store.profiles.value)
        assertEquals(sampleEmployee, store.activeProfile())
        val snapshot = storage.snapshot()
        assertTrue("auth.persisted_profiles" in snapshot)
        assertEquals(sampleEmployee.id, snapshot["auth.active_profile_id"])
        // Legacy key removed after migration.
        assertTrue("auth.persisted_user" !in snapshot)
    }
}
```

- [ ] **Step 6: Verify the tests fail to PASS (but compile)**

Run: `./gradlew :composeApp:compileTestKotlinAndroid` to confirm compilation.
Expected: BUILD SUCCESSFUL.

Run: `./gradlew :composeApp:testDebugUnitTest --tests "*PersistedSessionStoreTest*"`
Expected: tests fail with `NotImplementedError: implemented in Task 6`. This proves they exercise the real API.

- [ ] **Step 7: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/SessionStore.kt \
        composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt \
        composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt \
        composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt \
        composeApp/src/commonTest/
git commit -m "test(auth): add failing tests for multi-profile session store"
```

---

### Task 6: Implement multi-profile `PersistedSessionStore` (GREEN)

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt`

- [ ] **Step 1: Replace the `TODO()` stubs with the real implementation**

Replace the body of `PersistedSessionStore` (everything between the class declaration line and the final `}` of the companion) with:

```kotlin
class PersistedSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
    private val json: Json = DefaultJson,
) : SessionStore {

    private val _session = MutableStateFlow<AuthSession>(AuthSession.Unknown)
    override val session: StateFlow<AuthSession> = _session.asStateFlow()

    private val _profiles = MutableStateFlow<List<AppUser>>(emptyList())
    override val profiles: StateFlow<List<AppUser>> = _profiles.asStateFlow()

    private var activeId: String? = null

    private val profilesSerializer = ListSerializer(AppUser.serializer())

    override suspend fun hydrate() {
        migrateLegacyIfPresent()
        _profiles.value = readProfiles()
        activeId = storage.read(KEY_ACTIVE_PROFILE_ID)
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun addProfile(user: AppUser) {
        val updated = _profiles.value.filterNot { it.id == user.id } + user
        _profiles.value = updated
        storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, updated))
    }

    override suspend fun switchProfile(userId: String) {
        val target = _profiles.value.firstOrNull { it.id == userId }
            ?: throw IllegalArgumentException("Unknown profile id: $userId")
        activeId = userId
        storage.write(KEY_ACTIVE_PROFILE_ID, userId)
        if (_session.value is AuthSession.Authenticated) {
            _session.value = AuthSession.Authenticated(target)
        }
    }

    override suspend fun removeProfile(userId: String) {
        val updated = _profiles.value.filterNot { it.id == userId }
        _profiles.value = updated
        storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, updated))
        if (activeId == userId) {
            activeId = null
            storage.remove(KEY_ACTIVE_PROFILE_ID)
            _session.value = AuthSession.Unauthenticated
        }
    }

    override suspend fun setAuthenticated(userId: String) {
        val user = _profiles.value.firstOrNull { it.id == userId }
            ?: throw IllegalArgumentException("Unknown profile id: $userId")
        activeId = userId
        storage.write(KEY_ACTIVE_PROFILE_ID, userId)
        _session.value = AuthSession.Authenticated(user)
    }

    override suspend fun clear() {
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun forgetAllIdentities() {
        _profiles.value = emptyList()
        activeId = null
        storage.remove(KEY_PROFILES)
        storage.remove(KEY_ACTIVE_PROFILE_ID)
        _session.value = AuthSession.Unauthenticated
    }

    override suspend fun activeProfile(): AppUser? {
        val id = activeId ?: return null
        return _profiles.value.firstOrNull { it.id == id }
    }

    private suspend fun readProfiles(): List<AppUser> {
        val raw = storage.read(KEY_PROFILES) ?: return emptyList()
        return runCatching { json.decodeFromString(profilesSerializer, raw) }.getOrDefault(emptyList())
    }

    private suspend fun migrateLegacyIfPresent() {
        val legacy = storage.read(LEGACY_KEY_PERSISTED_USER) ?: return
        val user = runCatching { json.decodeFromString(AppUser.serializer(), legacy) }.getOrNull()
        if (user != null) {
            storage.write(KEY_PROFILES, json.encodeToString(profilesSerializer, listOf(user)))
            storage.write(KEY_ACTIVE_PROFILE_ID, user.id)
        }
        storage.remove(LEGACY_KEY_PERSISTED_USER)
    }

    internal companion object {
        const val KEY_PROFILES = "auth.persisted_profiles"
        const val KEY_ACTIVE_PROFILE_ID = "auth.active_profile_id"
        const val LEGACY_KEY_PERSISTED_USER = "auth.persisted_user"
        val DefaultJson = Json { ignoreUnknownKeys = true }
    }
}
```

Add this import at the top of the file:

```kotlin
import kotlinx.serialization.builtins.ListSerializer
```

- [ ] **Step 2: Run the tests and confirm GREEN**

Run: `./gradlew :composeApp:testDebugUnitTest --tests "*PersistedSessionStoreTest*"`
Expected: all 12 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/PersistedSessionStore.kt
git commit -m "feat(auth): multi-profile persisted session store"
```

---

### Task 7: Rewire `ConfirmAccountRoute` for the new API

The placeholder change in Task 5 already calls `addProfile` + `setAuthenticated(user.id)`. This task confirms that works end-to-end and removes the obsolete `AppUser` import that's no longer needed for the type annotation removed earlier.

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt`

- [ ] **Step 1: Read the file and confirm the calls**

The success path inside `repository.confirmAccount(...)`'s `.onSuccess` should already read:

```kotlin
.onSuccess { user ->
    state = state.copy(isLoading = false)
    sessionStore.addProfile(user)
    sessionStore.setAuthenticated(user.id)
}
```

If it doesn't (e.g., Task 5 wasn't completed correctly), make that edit now.

- [ ] **Step 2: Verify compile**

Run: `./gradlew :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 3: No commit needed unless an edit was required**

If Step 1 had to change anything, commit:

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt
git commit -m "feat(auth): registration adds + activates new profile"
```

---

### Task 8: Rewire `LoginRoute` for the new API

`finishAuthenticated()` is currently a placeholder. Restore it using the new API: read the active profile from `sessionStore`, defensively bail to reset if none exists.

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt`

- [ ] **Step 1: Replace `finishAuthenticated()` body**

Find the `suspend fun finishAuthenticated()` inside `LoginRoute`. Replace its body with:

```kotlin
suspend fun finishAuthenticated() {
    val active = sessionStore.activeProfile()
    if (active != null) {
        sessionStore.setAuthenticated(active.id)
    } else {
        securityRepository.reset()
        sessionStore.forgetAllIdentities()
        onResetSecureStorage()
    }
}
```

- [ ] **Step 2: Verify Android assemble**

Run: `./gradlew :composeApp:assembleDebug`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 3: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt
git commit -m "feat(auth): login activates persisted active profile"
```

---

### Task 9: Manual smoke verification

Tests cover the SessionStore behavior. UI changes still need a human in the loop.

- [ ] **Step 1: Install the debug build on a connected Android device or emulator**

Run: `./gradlew :composeApp:installDebug`
Expected: BUILD SUCCESSFUL and APK installed.

- [ ] **Step 2: Walk through the registration flow as a `RegularUser`**

In the app: Welcome → Register → enter code `ABC123XYZ` → confirm with a PIN.
Expected: lands on the existing `ActivePickupsScreen` (regular-user home), top bar shows initials "MH".

- [ ] **Step 3: Reset secure storage from the Profile menu (or via Login → "Reset secure storage")**

The full reset path: scroll until you find the "Ponastavi varno shrambo" entry in `LoginRoute` or trigger it however the UI exposes it today.
Expected: app returns to Welcome.

- [ ] **Step 4: Walk through the registration flow as an `Operator` employee**

Enter code `GHI789RST` and confirm with a PIN.
Expected: lands on the new **`OperatorHomeRoute`**, displaying "Pozdravljeni, Janez Kovač" and a single "Skeniraj QR kodo na paketniku" CTA. The button does nothing yet — that's expected.

- [ ] **Step 5: Cold-restart the app**

Kill and relaunch.
Expected: biometric prompt appears (or PIN fallback). After success, lands back on the Operator home with the same identity. Persistence is working.

- [ ] **Step 6: Verify the regression for `RecordManager`**

Reset, then register with code `DEF456UVW`.
Expected: also lands on the Operator home (since `RecordManager` is also `AppUser.Employee` — the role distinction doesn't matter for routing yet).

- [ ] **Step 7: Smoke test summary**

If any of steps 2–6 deviate from the expected behavior, file the deviation and stop — do not paper over with extra commits. The most likely failure modes are: navigation animation order (slide direction), top-bar initials calculation, or persistence not surviving the cold restart.

- [ ] **Step 8: Final commit (only if you fixed something in step 7)**

```bash
git add <files>
git commit -m "fix(operator): <one-line description>"
```

---

## What's NOT in this plan (deliberately deferred)

| Spec phase | Plan name (to be written separately) |
|---|---|
| 3 — Paketnik screen + fake operator repo | `2026-MM-DD-operator-paketnik-screen.md` |
| 4 — Insertion + removal flows | `2026-MM-DD-operator-insert-remove-flows.md` |
| 5 — Profile tab dropdown + audit log additions | `2026-MM-DD-operator-profile-and-audit.md` |
| 6 — Backend integration | `2026-MM-DD-operator-backend-wiring.md` |

The `OperatorHomeRoute` stub's `onScanQrClicked` callback is a `TODO` that points to the next plan's destination — the seam is intentional.

---

## Self-review notes

- All 6 in-scope spec items (routing branch, OperatorHome stub, multi-profile API, migration, Login/Confirm rewire, manual verification) have a corresponding task.
- No "TBD" / "TODO: implement later" steps. The one `TODO(...)` comment in App.kt is for the *next plan's* destination, and the plan explicitly notes that handoff.
- Type consistency: `addProfile`, `switchProfile`, `setAuthenticated(userId)`, `removeProfile`, `forgetAllIdentities`, `activeProfile()` — same names used across tasks 5, 6, 7, 8.
- File paths use repo-root–relative form, matching the existing CLAUDE.md command conventions.
