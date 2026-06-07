# Multiple Accounts on One Device — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let one device hold multiple registered ePrevzem accounts (e.g. an employee and a citizen identity), each with its own credentials, and show an account chooser before unlock when two or more accounts exist.

**Architecture:** The data layer (`SessionStore` with `List<AppUser>`) already stores multiple profiles. This plan makes the **credential layer per-account** by namespacing all secure-storage keys by account id (= backend `deviceId`): the security keypair/PIN/biometric (`SecurityRepository`) and the device id + tokens (`DeviceSessionStore`). Registration writes credentials to a staging namespace, then commits them under the real account id once the redeem response returns. A new account-chooser feature plus routing changes in `App.kt` complete the flow. The shared physical-device `fingerprint` stays global.

**Tech Stack:** Kotlin Multiplatform / Compose Multiplatform, kotlinx-coroutines-test, kotlinx-serialization, kotlin.test, Ktor MockEngine. Module: `composeApp` (run from `ePrevzemMobile/`).

**Spec:** `docs/superpowers/specs/2026-06-07-multi-account-on-device-design.md`

---

## Parallelization map

Tasks are grouped into **waves**. Tasks in the same wave touch disjoint files and can be dispatched to subagents concurrently. Later waves depend on interfaces frozen by earlier ones.

| Wave | Tasks | Depends on | Notes |
|------|-------|-----------|-------|
| **1** | **A** DeviceSessionStore per-account · **B** SecurityRepository per-account + staging/commit · **C** Account-picker feature UI | — | Fully parallel. Disjoint files. |
| **2** | **D** HttpAuthRepository token namespacing · **E** ConfirmAccountRoute staging→commit · **F** LoginRoute account-scoped | D→A · E→B · F→A,B | Parallel with each other. |
| **3** | **G** App.kt routing + chooser integration | C, F | Sequential, last. Integration. |

**Frozen interface contracts** (every task must implement these signatures exactly so parallel work composes):

```kotlin
// data/auth/DeviceSessionStore.kt  (Task A)
suspend fun saveSession(deviceId: String, accessToken: String, accessExpiresAt: String, refreshToken: String) // namespaces by deviceId
suspend fun deviceId(accountId: String): String?
suspend fun accessToken(accountId: String): String?
suspend fun refreshToken(accountId: String): String?
suspend fun fingerprint(): String            // GLOBAL — unchanged
suspend fun updateTokens(accountId: String, accessToken: String, accessExpiresAt: String, refreshToken: String)
suspend fun clear(accountId: String)

// data/security/SecurityRepository.kt  (Task B)
suspend fun isRegistered(accountId: String): Boolean
suspend fun isBiometricEnabled(accountId: String): Boolean
suspend fun register(pin: String, biometricEnabled: Boolean): Result<String>   // writes STAGING, returns public key PEM
suspend fun commitRegistration(accountId: String): Result<Unit>                // STAGING -> account namespace
suspend fun discardStaging()                                                   // wipe STAGING only
suspend fun enableBiometric(accountId: String, pin: String): Result<Unit>
suspend fun disableBiometric(accountId: String): Result<Unit>
suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit>
suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray>
suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray>
suspend fun reset(accountId: String)                                          // wipe ONE account's credentials

// feature/accountpicker/  (Task C)
sealed interface AccountPickerEvent {
    data class AccountSelected(val accountId: String) : AccountPickerEvent
    data object AddAccountClicked : AccountPickerEvent
}
```

**Note on unchanged code:** `HttpRegistrationRepository.confirmAccount` already calls `saveSession(deviceId = dto.deviceId, …)`; because `saveSession` keeps that signature and namespaces internally by `deviceId`, that file needs **no change**. `AppContainer` constructs single instances and needs **no change**.

---

## File Structure

**Modified — data layer**
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStore.kt` (Task A)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/SecurityRepository.kt` (Task B)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/LocalSecurityRepository.kt` (Task B)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/SecurityPorts.kt` (Task B — **create**)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/FakeSecurityRepository.kt` (Task B)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/HttpAuthRepository.kt` (Task D)

**Modified — feature/routing**
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt` (Task E)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt` (Task F)
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/App.kt` (Task G)

**Created — feature/accountpicker (Task C)**
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerState.kt`
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerEvent.kt`
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerScreen.kt`
- `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerRoute.kt`

**Tests**
- `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStoreTest.kt` (Task A — rewrite)
- `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/LocalSecurityRepositoryTest.kt` (Task B — create) + `FakeSecureStorage` test double
- `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerMapperTest.kt` (Task C — create)

**Build / verify commands** (run from `ePrevzemMobile/`, use `gradlew.bat` on Windows):
```
gradlew.bat :composeApp:compileCommonMainKotlinMetadata
gradlew.bat :composeApp:testDebugUnitTest
gradlew.bat :composeApp:testDebugUnitTest --tests "FQCN.testName"
```

---

# WAVE 1 — parallel

## Task A: DeviceSessionStore per-account namespacing

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStore.kt`
- Test: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStoreTest.kt` (rewrite)

**Depends on:** nothing. **Parallel with:** B, C.

- [ ] **Step 1: Rewrite the test for per-account isolation**

Replace the entire contents of `DeviceSessionStoreTest.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.auth

import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class DeviceSessionStoreTest {

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
```

- [ ] **Step 2: Run the test to verify it fails to compile / fails**

Run: `gradlew.bat :composeApp:testDebugUnitTest --tests "si.mentis.eprevzemmobile.data.auth.DeviceSessionStoreTest"`
Expected: FAIL — `deviceId()` etc. take no parameter yet (compile error / unresolved reference).

- [ ] **Step 3: Rewrite `DeviceSessionStore` to namespace by account id**

Replace the entire contents of `DeviceSessionStore.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.auth

import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import kotlin.random.Random

@OptIn(ExperimentalEncodingApi::class)
class DeviceSessionStore(
    private val storage: SessionStorage = SecureSessionStorage(),
) {
    suspend fun saveSession(
        deviceId: String,
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(deviceIdKey(deviceId), deviceId)
        storage.write(accessTokenKey(deviceId), accessToken)
        storage.write(accessExpiresKey(deviceId), accessExpiresAt)
        storage.write(refreshTokenKey(deviceId), refreshToken)
    }

    suspend fun deviceId(accountId: String): String? = storage.read(deviceIdKey(accountId))

    suspend fun accessToken(accountId: String): String? = storage.read(accessTokenKey(accountId))

    suspend fun refreshToken(accountId: String): String? = storage.read(refreshTokenKey(accountId))

    suspend fun fingerprint(): String {
        val existing = storage.read(KEY_FINGERPRINT)
        if (existing != null) {
            return existing
        }
        val fingerprint = Base64.encode(Random.nextBytes(16))
        storage.write(KEY_FINGERPRINT, fingerprint)
        return fingerprint
    }

    suspend fun updateTokens(
        accountId: String,
        accessToken: String,
        accessExpiresAt: String,
        refreshToken: String,
    ) {
        storage.write(accessTokenKey(accountId), accessToken)
        storage.write(accessExpiresKey(accountId), accessExpiresAt)
        storage.write(refreshTokenKey(accountId), refreshToken)
    }

    suspend fun clear(accountId: String) {
        storage.remove(deviceIdKey(accountId))
        storage.remove(accessTokenKey(accountId))
        storage.remove(accessExpiresKey(accountId))
        storage.remove(refreshTokenKey(accountId))
    }

    private companion object {
        const val KEY_FINGERPRINT = "auth.device_fingerprint"

        fun deviceIdKey(accountId: String) = "auth.$accountId.device_id"
        fun accessTokenKey(accountId: String) = "auth.$accountId.access_token"
        fun accessExpiresKey(accountId: String) = "auth.$accountId.access_expires"
        fun refreshTokenKey(accountId: String) = "auth.$accountId.refresh_token"
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `gradlew.bat :composeApp:testDebugUnitTest --tests "si.mentis.eprevzemmobile.data.auth.DeviceSessionStoreTest"`
Expected: PASS (6 tests).

> NOTE for the integrating agent: this changes `deviceId()`/`accessToken()`/`refreshToken()`/`updateTokens()`/`clear()` signatures. `HttpAuthRepository` (Task D) and `LoginRoute` (Task F) are the callers and are updated in Wave 2. `HttpRegistrationRepository` is unaffected (still calls `saveSession(deviceId = …)`).

- [ ] **Step 5: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStore.kt composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/auth/DeviceSessionStoreTest.kt
git commit -m "feat(mobile): per-account device session storage"
```

---

## Task B: SecurityRepository per-account + staging→commit

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/SecurityRepository.kt`
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/LocalSecurityRepository.kt`
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/FakeSecurityRepository.kt`
- Create: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/FakeSecureStorage.kt`
- Create: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/LocalSecurityRepositoryTest.kt`

**Depends on:** nothing. **Parallel with:** A, C.

> Background: `SecureStorage`, `SecurityCrypto`, and `BiometricAuthenticator` are all `expect class`es — they cannot be subclassed or instantiated in commonTest. So this task **introduces three narrow port interfaces** that `LocalSecurityRepository` depends on (`SecurityKeyStore`, `SecurityCryptoPort`, `BiometricGate`), each with a production adapter wrapping the corresponding `expect class` — mirroring how `SessionStorage`/`SecureSessionStorage` already wrap `SecureStorage`. Tests then exercise the staging→commit key plumbing with in-memory fakes of all three ports.

- [ ] **Step 1: Update the `SecurityRepository` interface**

Replace the entire contents of `SecurityRepository.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.security

interface SecurityRepository {
    suspend fun isRegistered(accountId: String): Boolean
    suspend fun isBiometricEnabled(accountId: String): Boolean

    /** Generates a keypair and writes credentials to a staging namespace. Returns the public key PEM. */
    suspend fun register(pin: String, biometricEnabled: Boolean): Result<String>

    /** Promotes staged credentials to [accountId]'s namespace and clears staging. */
    suspend fun commitRegistration(accountId: String): Result<Unit>

    /** Removes any staged credentials without touching committed accounts. */
    suspend fun discardStaging()

    suspend fun enableBiometric(accountId: String, pin: String): Result<Unit>
    suspend fun disableBiometric(accountId: String): Result<Unit>
    suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit>
    suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray>
    suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray>

    /** Wipes one account's credentials. Other accounts are unaffected. */
    suspend fun reset(accountId: String)
}
```

- [ ] **Step 2: Introduce the three port seams used by `LocalSecurityRepository`**

Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/SecurityPorts.kt` with the storage, crypto, and biometric ports plus their production adapters over the `expect class`es. (A separate file keeps the seam reusable and `LocalSecurityRepository.kt` focused.)

```kotlin
package si.mentis.eprevzemmobile.data.security

/**
 * Narrow secure-storage seam used by [LocalSecurityRepository] so tests can
 * substitute an in-memory implementation. Production wraps [SecureStorage].
 */
interface SecurityKeyStore {
    suspend fun readString(key: String): String?
    suspend fun writeString(key: String, value: String)
    suspend fun remove(key: String)
    suspend fun readBiometricString(key: String): String?
    suspend fun writeBiometricString(key: String, value: String)
    suspend fun removeAll(prefix: String)
}

class SecureStorageKeyStore(
    private val storage: SecureStorage = SecureStorage(),
) : SecurityKeyStore {
    override suspend fun readString(key: String) = storage.readString(key)
    override suspend fun writeString(key: String, value: String) = storage.writeString(key, value)
    override suspend fun remove(key: String) = storage.remove(key)
    override suspend fun readBiometricString(key: String) = storage.readBiometricString(key)
    override suspend fun writeBiometricString(key: String, value: String) = storage.writeBiometricString(key, value)
    // SecureStorage has no prefix scan; remove the known suffixes for the prefix's account.
    override suspend fun removeAll(prefix: String) {
        storage.remove("$prefix.$SUFFIX_PUBLIC")
        storage.remove("$prefix.$SUFFIX_CIPHERTEXT")
        storage.remove("$prefix.$SUFFIX_NONCE")
        storage.remove("$prefix.$SUFFIX_SALT")
        storage.remove("$prefix.$SUFFIX_BIOMETRIC")
    }
}

/** Crypto seam — production wraps the [SecurityCrypto] expect class (which cannot be faked directly). */
interface SecurityCryptoPort {
    fun randomBytes(size: Int): ByteArray
    fun generateEcdsaKeyPair(): EcdsaKeyPair
    fun deriveAesKey(pin: String, salt: ByteArray): ByteArray
    fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload
    fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray
    fun signEcdsa(privateKey: ByteArray, challenge: ByteArray): ByteArray
}

class DefaultSecurityCrypto(
    private val crypto: SecurityCrypto = SecurityCrypto(),
) : SecurityCryptoPort {
    override fun randomBytes(size: Int) = crypto.randomBytes(size)
    override fun generateEcdsaKeyPair() = crypto.generateEcdsaKeyPair()
    override fun deriveAesKey(pin: String, salt: ByteArray) = crypto.deriveAesKey(pin, salt)
    override fun encryptAesGcm(key: ByteArray, plaintext: ByteArray) = crypto.encryptAesGcm(key, plaintext)
    override fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload) = crypto.decryptAesGcm(key, payload)
    override fun signEcdsa(privateKey: ByteArray, challenge: ByteArray) = crypto.signEcdsa(privateKey, challenge)
}

/** Biometric seam — production wraps the [BiometricAuthenticator] expect class. */
interface BiometricGate {
    suspend fun authenticate(reason: String): Boolean
}

class DefaultBiometricGate(
    private val authenticator: BiometricAuthenticator = BiometricAuthenticator(),
) : BiometricGate {
    override suspend fun authenticate(reason: String) = authenticator.authenticate(reason)
}

internal const val SUFFIX_PUBLIC = "public_key_pem"
internal const val SUFFIX_CIPHERTEXT = "private_key_ciphertext"
internal const val SUFFIX_NONCE = "private_key_nonce"
internal const val SUFFIX_SALT = "pin_salt"
internal const val SUFFIX_BIOMETRIC = "biometric_aes_key"
internal const val STAGING_ID = "__staging__"
```

- [ ] **Step 3: Rewrite the body of `LocalSecurityRepository` to be account-scoped**

Replace the `LocalSecurityRepository` class (the `class LocalSecurityRepository(...) : SecurityRepository { ... }` block and the old top-of-file `private const val KEY_...` lines) with:

```kotlin
class LocalSecurityRepository(
    private val crypto: SecurityCryptoPort = DefaultSecurityCrypto(),
    private val storage: SecurityKeyStore = SecureStorageKeyStore(),
    private val biometricAuthenticator: BiometricGate = DefaultBiometricGate(),
) : SecurityRepository {

    private fun key(accountId: String, suffix: String) = "security.$accountId.$suffix"

    override suspend fun isRegistered(accountId: String): Boolean = runCatching {
        storage.readString(key(accountId, SUFFIX_PUBLIC)) != null &&
            storage.readString(key(accountId, SUFFIX_CIPHERTEXT)) != null &&
            storage.readString(key(accountId, SUFFIX_NONCE)) != null &&
            storage.readString(key(accountId, SUFFIX_SALT)) != null
    }.getOrElse { false }

    override suspend fun isBiometricEnabled(accountId: String): Boolean = runCatching {
        storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC)) != null
    }.getOrElse { false }

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> = runCatching {
        val keyPair = crypto.generateEcdsaKeyPair()
        val salt = crypto.randomBytes(size = 16)
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val encryptedPrivateKey = crypto.encryptAesGcm(aesKey, keyPair.privateKeyBytes)

        storage.writeString(key(STAGING_ID, SUFFIX_PUBLIC), keyPair.publicKeyPem)
        storage.writeString(key(STAGING_ID, SUFFIX_SALT), salt.toBase64())
        storage.writeString(key(STAGING_ID, SUFFIX_CIPHERTEXT), encryptedPrivateKey.ciphertext.toBase64())
        storage.writeString(key(STAGING_ID, SUFFIX_NONCE), encryptedPrivateKey.nonce.toBase64())

        if (biometricEnabled) {
            val enrolled = runCatching {
                val authenticated = biometricAuthenticator.authenticate(
                    "Aktivirajte biometrično zaščito za ePrevzem"
                )
                if (authenticated) {
                    storage.writeBiometricString(key(STAGING_ID, SUFFIX_BIOMETRIC), aesKey.toBase64())
                }
                authenticated
            }.getOrElse { false }
            if (!enrolled) {
                storage.remove(key(STAGING_ID, SUFFIX_BIOMETRIC))
            }
        } else {
            storage.remove(key(STAGING_ID, SUFFIX_BIOMETRIC))
        }

        keyPair.publicKeyPem
    }

    override suspend fun commitRegistration(accountId: String): Result<Unit> = runCatching {
        val pub = storage.readString(key(STAGING_ID, SUFFIX_PUBLIC)) ?: throw SecurityNotRegisteredException()
        val cipher = storage.readString(key(STAGING_ID, SUFFIX_CIPHERTEXT)) ?: throw SecurityNotRegisteredException()
        val nonce = storage.readString(key(STAGING_ID, SUFFIX_NONCE)) ?: throw SecurityNotRegisteredException()
        val salt = storage.readString(key(STAGING_ID, SUFFIX_SALT)) ?: throw SecurityNotRegisteredException()
        val bio = storage.readBiometricString(key(STAGING_ID, SUFFIX_BIOMETRIC))

        storage.writeString(key(accountId, SUFFIX_PUBLIC), pub)
        storage.writeString(key(accountId, SUFFIX_CIPHERTEXT), cipher)
        storage.writeString(key(accountId, SUFFIX_NONCE), nonce)
        storage.writeString(key(accountId, SUFFIX_SALT), salt)
        if (bio != null) {
            storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), bio)
        } else {
            storage.remove(key(accountId, SUFFIX_BIOMETRIC))
        }
        storage.removeAll("security.$STAGING_ID")
    }

    override suspend fun discardStaging() {
        storage.removeAll("security.$STAGING_ID")
    }

    override suspend fun enableBiometric(accountId: String, pin: String): Result<Unit> = runCatching {
        val salt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        decryptStoredPrivateKey(accountId, aesKey)

        val authenticated = biometricAuthenticator.authenticate(
            "Aktivirajte biometrično zaščito za ePrevzem"
        )
        if (!authenticated) throw BiometricAuthenticationException()

        storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), aesKey.toBase64())
    }.recoverCatching { error ->
        when (error) {
            is SecurityNotRegisteredException,
            is BiometricAuthenticationException -> throw error
            else -> throw InvalidPinException()
        }
    }

    override suspend fun disableBiometric(accountId: String): Result<Unit> = runCatching {
        storage.remove(key(accountId, SUFFIX_BIOMETRIC))
    }

    override suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit> = runCatching {
        val currentSalt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val currentAesKey = crypto.deriveAesKey(pin = currentPin, salt = currentSalt)
        val privateKey = decryptStoredPrivateKey(accountId, currentAesKey)
        val biometricEnabled = storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC)) != null

        val newSalt = crypto.randomBytes(size = 16)
        val newAesKey = crypto.deriveAesKey(pin = newPin, salt = newSalt)
        val encryptedPrivateKey = crypto.encryptAesGcm(newAesKey, privateKey)

        storage.writeString(key(accountId, SUFFIX_SALT), newSalt.toBase64())
        storage.writeString(key(accountId, SUFFIX_CIPHERTEXT), encryptedPrivateKey.ciphertext.toBase64())
        storage.writeString(key(accountId, SUFFIX_NONCE), encryptedPrivateKey.nonce.toBase64())
        if (biometricEnabled) {
            storage.writeBiometricString(key(accountId, SUFFIX_BIOMETRIC), newAesKey.toBase64())
        }
    }.recoverCatching { error ->
        if (error is SecurityNotRegisteredException) throw error
        throw InvalidPinException()
    }

    override suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray> = runCatching {
        val salt = storage.readString(key(accountId, SUFFIX_SALT))?.fromBase64() ?: throw SecurityNotRegisteredException()
        val aesKey = crypto.deriveAesKey(pin = pin, salt = salt)
        val privateKey = decryptStoredPrivateKey(accountId, aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }.recoverCatching { error ->
        if (error is SecurityNotRegisteredException) throw error
        throw InvalidPinException()
    }

    override suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray> = runCatching {
        val authenticated = biometricAuthenticator.authenticate("Potrdite identiteto za podpis izziva")
        if (!authenticated) throw BiometricAuthenticationException()

        val aesKey = storage.readBiometricString(key(accountId, SUFFIX_BIOMETRIC))?.fromBase64()
            ?: throw BiometricAuthenticationException()
        val privateKey = decryptStoredPrivateKey(accountId, aesKey)
        crypto.signEcdsa(privateKey, challenge)
    }

    private suspend fun decryptStoredPrivateKey(accountId: String, aesKey: ByteArray): ByteArray {
        val ciphertext = storage.readString(key(accountId, SUFFIX_CIPHERTEXT))?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        val nonce = storage.readString(key(accountId, SUFFIX_NONCE))?.fromBase64()
            ?: throw SecurityNotRegisteredException()
        return crypto.decryptAesGcm(aesKey, EncryptedPayload(ciphertext, nonce))
    }

    override suspend fun reset(accountId: String) {
        storage.removeAll("security.$accountId")
    }
}
```

- [ ] **Step 4: Update `FakeSecurityRepository` to the new interface**

Replace the entire contents of `FakeSecurityRepository.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.security

class FakeSecurityRepository : SecurityRepository {
    private data class Cred(val pin: String, val biometric: Boolean)

    private val accounts = mutableMapOf<String, Cred>()
    private var staged: Cred? = null
    private var stagedPublicKey: String = ""

    override suspend fun isRegistered(accountId: String): Boolean = accounts.containsKey(accountId)

    override suspend fun isBiometricEnabled(accountId: String): Boolean = accounts[accountId]?.biometric ?: false

    override suspend fun register(pin: String, biometricEnabled: Boolean): Result<String> {
        staged = Cred(pin, biometricEnabled)
        stagedPublicKey = "-----BEGIN PUBLIC KEY-----\nfake-public-key-biometric-$biometricEnabled\n-----END PUBLIC KEY-----"
        return Result.success(stagedPublicKey)
    }

    override suspend fun commitRegistration(accountId: String): Result<Unit> {
        val cred = staged ?: return Result.failure(SecurityNotRegisteredException())
        accounts[accountId] = cred
        staged = null
        return Result.success(Unit)
    }

    override suspend fun discardStaging() {
        staged = null
    }

    override suspend fun enableBiometric(accountId: String, pin: String): Result<Unit> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (pin != cred.pin) return Result.failure(InvalidPinException())
        accounts[accountId] = cred.copy(biometric = true)
        return Result.success(Unit)
    }

    override suspend fun disableBiometric(accountId: String): Result<Unit> {
        accounts[accountId]?.let { accounts[accountId] = it.copy(biometric = false) }
        return Result.success(Unit)
    }

    override suspend fun changePin(accountId: String, currentPin: String, newPin: String): Result<Unit> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (currentPin != cred.pin) return Result.failure(InvalidPinException())
        accounts[accountId] = cred.copy(pin = newPin)
        return Result.success(Unit)
    }

    override suspend fun signChallengeWithPin(accountId: String, pin: String, challenge: ByteArray): Result<ByteArray> {
        val cred = accounts[accountId] ?: return Result.failure(SecurityNotRegisteredException())
        if (pin != cred.pin) return Result.failure(InvalidPinException())
        return Result.success("fake-pin-signature:${challenge.toBase64()}".encodeToByteArray())
    }

    override suspend fun signChallengeWithBiometric(accountId: String, challenge: ByteArray): Result<ByteArray> {
        if (!accounts.containsKey(accountId)) return Result.failure(SecurityNotRegisteredException())
        return Result.success("fake-biometric-signature:${challenge.toBase64()}".encodeToByteArray())
    }

    override suspend fun reset(accountId: String) {
        accounts.remove(accountId)
    }
}
```

- [ ] **Step 5: Create the in-memory `SecurityKeyStore` test double**

Create `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/FakeSecureStorage.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.security

class FakeSecurityKeyStore : SecurityKeyStore {
    val plain = mutableMapOf<String, String>()
    val biometric = mutableMapOf<String, String>()

    override suspend fun readString(key: String): String? = plain[key]
    override suspend fun writeString(key: String, value: String) { plain[key] = value }
    override suspend fun remove(key: String) { plain.remove(key) }
    override suspend fun readBiometricString(key: String): String? = biometric[key]
    override suspend fun writeBiometricString(key: String, value: String) { biometric[key] = value }
    override suspend fun removeAll(prefix: String) {
        plain.keys.filter { it.startsWith("$prefix.") }.forEach { plain.remove(it) }
        biometric.keys.filter { it.startsWith("$prefix.") }.forEach { biometric.remove(it) }
    }
}

/** Deterministic crypto so staging→commit plumbing is verifiable without real crypto. */
class FakeSecurityCrypto : SecurityCryptoPort {
    override fun generateEcdsaKeyPair(): EcdsaKeyPair =
        EcdsaKeyPair(publicKeyPem = "PUB", privateKeyBytes = "PRIV".encodeToByteArray())
    override fun randomBytes(size: Int): ByteArray = ByteArray(size) { 1 }
    override fun deriveAesKey(pin: String, salt: ByteArray): ByteArray = ("AES:$pin").encodeToByteArray()
    override fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload =
        EncryptedPayload(ciphertext = plaintext, nonce = "NONCE".encodeToByteArray())
    override fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray = payload.ciphertext
    override fun signEcdsa(privateKey: ByteArray, challenge: ByteArray): ByteArray =
        ("SIG:" + challenge.decodeToString()).encodeToByteArray()
}

class FakeBiometricGate(var result: Boolean = true) : BiometricGate {
    override suspend fun authenticate(reason: String): Boolean = result
}
```

- [ ] **Step 6: Create the test for staging→commit and per-account isolation**

Create `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/LocalSecurityRepositoryTest.kt`:

```kotlin
package si.mentis.eprevzemmobile.data.security

import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class LocalSecurityRepositoryTest {

    private fun repo(storage: FakeSecurityKeyStore) = LocalSecurityRepository(
        crypto = FakeSecurityCrypto(),
        storage = storage,
        biometricAuthenticator = FakeBiometricGate(result = true),
    )

    @Test
    fun register_writes_only_staging_not_account() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)

        repo.register(pin = "123456", biometricEnabled = false).getOrThrow()

        assertTrue(storage.plain.keys.any { it.startsWith("security.__staging__.") })
        assertFalse(repo.isRegistered("acc-1"))
    }

    @Test
    fun commit_promotes_staging_to_account_and_clears_staging() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)
        repo.register(pin = "123456", biometricEnabled = false).getOrThrow()

        repo.commitRegistration("acc-1").getOrThrow()

        assertTrue(repo.isRegistered("acc-1"))
        assertFalse(storage.plain.keys.any { it.startsWith("security.__staging__.") })
    }

    @Test
    fun discardStaging_leaves_committed_accounts_intact() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)
        repo.register("111111", false).getOrThrow()
        repo.commitRegistration("acc-1").getOrThrow()

        repo.register("222222", false).getOrThrow()
        repo.discardStaging()

        assertTrue(repo.isRegistered("acc-1"))
        assertFalse(storage.plain.keys.any { it.startsWith("security.__staging__.") })
    }

    @Test
    fun two_accounts_coexist_and_reset_one_keeps_the_other() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)
        repo.register("111111", false).getOrThrow()
        repo.commitRegistration("acc-1").getOrThrow()
        repo.register("222222", false).getOrThrow()
        repo.commitRegistration("acc-2").getOrThrow()

        repo.reset("acc-1")

        assertFalse(repo.isRegistered("acc-1"))
        assertTrue(repo.isRegistered("acc-2"))
    }

    @Test
    fun signChallengeWithPin_succeeds_for_committed_account() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)
        repo.register("123456", false).getOrThrow()
        repo.commitRegistration("acc-1").getOrThrow()

        val sig = repo.signChallengeWithPin("acc-1", "123456", "hello".encodeToByteArray())

        assertTrue(sig.isSuccess)
        assertEquals("SIG:hello", sig.getOrThrow().decodeToString())
    }

    @Test
    fun signChallengeWithPin_wrong_pin_fails() = runTest {
        val storage = FakeSecurityKeyStore()
        val repo = repo(storage)
        repo.register("123456", false).getOrThrow()
        repo.commitRegistration("acc-1").getOrThrow()

        val sig = repo.signChallengeWithPin("acc-1", "000000", "hello".encodeToByteArray())

        assertTrue(sig.isFailure)
    }
}
```

- [ ] **Step 7: Run Task B tests to verify they pass**

Run: `gradlew.bat :composeApp:testDebugUnitTest --tests "si.mentis.eprevzemmobile.data.security.LocalSecurityRepositoryTest"`
Expected: PASS (6 tests).

- [ ] **Step 8: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/ composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/data/security/
git commit -m "feat(mobile): per-account security credentials with staging commit"
```

---

## Task C: Account-picker feature UI

**Files:**
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerState.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerEvent.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerScreen.kt`
- Create: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerRoute.kt`
- Create: `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerMapperTest.kt`

**Depends on:** nothing (uses only `AppUser` + `SessionStore`, both already present). **Parallel with:** A, B.

Design-system rules: token-only styling, `E*` components, `Painter` icons from `EPrevzemIcons`, Slovenian UI text, stateless `Screen` / stateful `Route`. The clickable account rows are private composables inside the screen file (not new public components).

- [ ] **Step 1: Write the failing mapper test**

Create `composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerMapperTest.kt`:

```kotlin
package si.mentis.eprevzemmobile.feature.accountpicker

import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class AccountPickerMapperTest {

    private val citizen = AppUser.RegularUser(
        id = "u-1", fullName = "Marko Horvat", email = "m@x.si", phone = "+386 41 000 001",
    )
    private val employee = AppUser.Employee(
        id = "u-2", fullName = "Ana Novak", email = "a@x.si", phone = "+386 41 000 002",
        status = "Aktiven", validUntil = "31. dec 2026",
        organizationId = "org-1", organizationName = "MNZ", organizationType = "Državni organ",
        organizationLocation = "Štefanova 2", roles = listOf(EmployeeRole.Operator),
    )

    @Test
    fun maps_citizen_to_citizen_row_without_organization() {
        val rows = listOf(citizen).toAccountRows()
        assertEquals(1, rows.size)
        assertEquals("u-1", rows[0].id)
        assertEquals("Marko Horvat", rows[0].fullName)
        assertEquals(AccountType.Citizen, rows[0].type)
        assertNull(rows[0].organizationName)
    }

    @Test
    fun maps_employee_to_employee_row_with_organization() {
        val rows = listOf(employee).toAccountRows()
        assertEquals(AccountType.Employee, rows[0].type)
        assertEquals("MNZ", rows[0].organizationName)
    }

    @Test
    fun preserves_order() {
        val rows = listOf(citizen, employee).toAccountRows()
        assertEquals(listOf("u-1", "u-2"), rows.map { it.id })
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `gradlew.bat :composeApp:testDebugUnitTest --tests "si.mentis.eprevzemmobile.feature.accountpicker.AccountPickerMapperTest"`
Expected: FAIL — unresolved references `toAccountRows`, `AccountType`, `AccountRow`.

- [ ] **Step 3: Create the state + mapper**

Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerState.kt`:

```kotlin
package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.domain.AppUser

enum class AccountType { Employee, Citizen }

@Immutable
data class AccountRow(
    val id: String,
    val fullName: String,
    val type: AccountType,
    val organizationName: String?,
)

@Immutable
data class AccountPickerState(
    val accounts: List<AccountRow> = emptyList(),
)

fun List<AppUser>.toAccountRows(): List<AccountRow> = map { user ->
    when (user) {
        is AppUser.Employee -> AccountRow(
            id = user.id,
            fullName = user.fullName,
            type = AccountType.Employee,
            organizationName = user.organizationName,
        )
        is AppUser.RegularUser -> AccountRow(
            id = user.id,
            fullName = user.fullName,
            type = AccountType.Citizen,
            organizationName = null,
        )
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `gradlew.bat :composeApp:testDebugUnitTest --tests "si.mentis.eprevzemmobile.feature.accountpicker.AccountPickerMapperTest"`
Expected: PASS (3 tests).

- [ ] **Step 5: Create the event type**

Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerEvent.kt`:

```kotlin
package si.mentis.eprevzemmobile.feature.accountpicker

sealed interface AccountPickerEvent {
    data class AccountSelected(val accountId: String) : AccountPickerEvent
    data object AddAccountClicked : AccountPickerEvent
}
```

- [ ] **Step 6: Create the stateless screen**

Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerScreen.kt`:

```kotlin
package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object AccountPickerStrings {
    const val Title = "Izberite račun"
    const val Subtitle = "Na tej napravi imate shranjenih več računov. Izberite, s katerim se želite prijaviti."
    const val Employee = "Zaposleni"
    const val Citizen = "Občan"
    const val AddAccount = "Dodaj račun"
}

@Composable
fun AccountPickerScreen(
    state: AccountPickerState,
    onEvent: (AccountPickerEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = AccountPickerStrings.Title,
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(
                        horizontal = EPrevzemTheme.spacing.screenHorizontal,
                        vertical = EPrevzemTheme.spacing.md,
                    ),
            ) {
                ESecondaryButton(
                    label = AccountPickerStrings.AddAccount,
                    icon = EPrevzemIcons.arrowRight(),
                    onClick = { onEvent(AccountPickerEvent.AddAccountClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        EScreen(verticalGap = EPrevzemTheme.spacing.md) {
            Text(
                text = AccountPickerStrings.Subtitle,
                style = EPrevzemTheme.typography.body,
                color = EPrevzemTheme.colors.textSecondary,
            )
            state.accounts.forEach { account ->
                AccountRowItem(
                    account = account,
                    onClick = { onEvent(AccountPickerEvent.AccountSelected(account.id)) },
                )
            }
        }
    }
}

@Composable
private fun AccountRowItem(
    account: AccountRow,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val shape = EPrevzemTheme.shapes.large
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.md),
        modifier = modifier
            .fillMaxWidth()
            .clip(shape)
            .background(colors.surface)
            .border(1.dp, colors.border, shape)
            .clickable(onClick = onClick)
            .padding(EPrevzemTheme.spacing.md),
    ) {
        EIconChip(
            painter = EPrevzemIcons.profile(),
            tint = if (account.type == AccountType.Employee) EIconTint.Teal else EIconTint.Green,
        )
        Column(
            verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.xs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = account.fullName,
                style = typo.body.copy(fontWeight = FontWeight.SemiBold),
                color = colors.textPrimary,
            )
            AccountTypeLabel(account = account)
        }
        Icon(
            painter = EPrevzemIcons.arrowRight(),
            contentDescription = null,
            tint = colors.textSecondary,
            modifier = Modifier.size(20.dp),
        )
    }
}

@Composable
private fun AccountTypeLabel(account: AccountRow) {
    val colors = EPrevzemTheme.colors
    val typeText = when (account.type) {
        AccountType.Employee -> AccountPickerStrings.Employee
        AccountType.Citizen -> AccountPickerStrings.Citizen
    }
    val label = account.organizationName
        ?.takeIf { it.isNotBlank() }
        ?.let { "$typeText · $it" }
        ?: typeText
    Row(
        modifier = Modifier
            .clip(RoundedCornerShape(50))
            .background(colors.surfaceMuted)
            .padding(horizontal = 10.dp, vertical = 4.dp),
    ) {
        Text(
            text = label,
            style = EPrevzemTheme.typography.caption.copy(fontWeight = FontWeight.SemiBold),
            color = colors.textSecondary,
        )
    }
}
```

> Token check before committing: confirm `EPrevzemTheme.spacing.xs`, `spacing.md`, `spacing.screenHorizontal`, `shapes.large`, `colors.surface`, `colors.surfaceMuted`, `colors.border`, `colors.textPrimary`, `colors.textSecondary`, `typography.body/caption` all exist (they are used in existing screens — see `WelcomeScreen.kt`, `EStatusChip.kt`, `ECardBase.kt`). If a token name differs, use the existing equivalent — do not hardcode.

- [ ] **Step 7: Create the stateful route**

Create `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/AccountPickerRoute.kt`:

```kotlin
package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.auth.SessionStore

@Composable
fun AccountPickerRoute(
    onAccountSelected: (String) -> Unit,
    onAddAccount: () -> Unit,
    sessionStore: SessionStore = AppContainer.sessionStore,
    modifier: Modifier = Modifier,
) {
    val profiles by sessionStore.profiles.collectAsState()
    val state = AccountPickerState(accounts = profiles.toAccountRows())

    AccountPickerScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                is AccountPickerEvent.AccountSelected -> onAccountSelected(event.accountId)
                AccountPickerEvent.AddAccountClicked -> onAddAccount()
            }
        },
    )
}
```

- [ ] **Step 8: Compile check**

Run: `gradlew.bat :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 9: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/ composeApp/src/commonTest/kotlin/si/mentis/eprevzemmobile/feature/accountpicker/
git commit -m "feat(mobile): account picker screen"
```

---

# WAVE 2 — parallel (after Wave 1)

## Task D: HttpAuthRepository per-account tokens

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/HttpAuthRepository.kt`

**Depends on:** A. **Parallel with:** E, F.

The `AuthRepository` interface is unchanged (`getChallenge(deviceId)`, `verifySignature(deviceId, signature)`); only the internal `updateTokens` call must pass the account namespace, which equals `deviceId`.

- [ ] **Step 1: Pass `deviceId` as the account id to `updateTokens`**

In `HttpAuthRepository.kt`, find the `verifySignature` body's token write:

```kotlin
                sessionStore.updateTokens(
                    accessToken = dto.accessToken,
                    accessExpiresAt = dto.accessTokenExpiresAt,
                    refreshToken = dto.refreshToken,
                )
```

Replace with:

```kotlin
                sessionStore.updateTokens(
                    accountId = deviceId,
                    accessToken = dto.accessToken,
                    accessExpiresAt = dto.accessTokenExpiresAt,
                    refreshToken = dto.refreshToken,
                )
```

- [ ] **Step 2: Compile check**

Run: `gradlew.bat :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL.

- [ ] **Step 3: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/data/security/HttpAuthRepository.kt
git commit -m "feat(mobile): scope refreshed tokens to account"
```

---

## Task E: ConfirmAccountRoute staging→commit wiring

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt`

**Depends on:** B. **Parallel with:** D, F.

Registration must: `register` (staging) → `confirmAccount` → `commitRegistration(user.id)` → `addProfile` + `setAuthenticated`. On **any** failure, call `discardStaging()` — never `forgetAllIdentities()` (other accounts must survive).

- [ ] **Step 1: Replace the `SubmitClicked` handler body**

In `ConfirmAccountRoute.kt`, replace the entire `ConfirmAccountEvent.SubmitClicked -> { ... }` branch with:

```kotlin
                ConfirmAccountEvent.SubmitClicked -> {
                    if (state.canSubmit && !state.isLoading) {
                        state = state.copy(isLoading = true, error = null)
                        scope.launch {
                            securityRepository.register(state.pin, state.isBiometricEnabled)
                                .onSuccess { publicKey ->
                                    repository.confirmAccount(validatedCode, publicKey)
                                        .onSuccess { user ->
                                            val committed = runCatching {
                                                securityRepository.commitRegistration(user.id).getOrThrow()
                                                sessionStore.addProfile(user)
                                                sessionStore.setAuthenticated(user.id)
                                            }
                                            committed
                                                .onSuccess { state = state.copy(isLoading = false) }
                                                .onFailure {
                                                    securityRepository.discardStaging()
                                                    securityRepository.reset(user.id)
                                                    state = state.copy(
                                                        isLoading = false,
                                                        error = "Registracija ni uspela. Poskusite znova.",
                                                    )
                                                }
                                        }
                                        .onFailure {
                                            securityRepository.discardStaging()
                                            state = state.copy(
                                                isLoading = false,
                                                error = "Registracija ni uspela. Poskusite znova.",
                                            )
                                        }
                                }
                                .onFailure {
                                    securityRepository.discardStaging()
                                    state = state.copy(
                                        isLoading = false,
                                        error = "Varnostna nastavitev ni uspela: ${it::class.simpleName}: ${it.message}",
                                    )
                                }
                        }
                    }
                }
```

- [ ] **Step 2: Compile check**

Run: `gradlew.bat :composeApp:compileCommonMainKotlinMetadata`
Expected: BUILD SUCCESSFUL. (No more references to the removed `securityRepository.reset()` no-arg or `forgetAllIdentities()` in this file.)

- [ ] **Step 3: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/registration/confirm/ConfirmAccountRoute.kt
git commit -m "feat(mobile): commit per-account credentials on registration"
```

---

## Task F: LoginRoute account-scoped unlock

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt`

**Depends on:** A, B. **Parallel with:** D, E.

`LoginRoute` gains an `accountId` parameter. It signs that account's challenge with that account's key + that account's `deviceId`, then `setAuthenticated(accountId)`. The reset path resets only that account.

- [ ] **Step 1: Add `accountId` parameter and rewrite the auth helpers**

Replace the function signature and the three helpers `finishAuthenticated`, `authWithBiometric`, `authWithPin` in `LoginRoute.kt`.

New signature (add `accountId` as the first parameter):

```kotlin
@Composable
fun LoginRoute(
    accountId: String,
    onResetSecureStorage: () -> Unit,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    authRepository: AuthRepository = AppContainer.authRepository,
    sessionStore: SessionStore = AppContainer.sessionStore,
    deviceSessionStore: DeviceSessionStore = AppContainer.deviceSessionStore,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(LoginState()) }
    val scope = rememberCoroutineScope()

    suspend fun finishAuthenticated() {
        sessionStore.setAuthenticated(accountId)
    }

    fun resetThisAccount() {
        scope.launch {
            securityRepository.reset(accountId)
            sessionStore.removeProfile(accountId)
            onResetSecureStorage()
        }
    }

    fun authWithBiometric() {
        scope.launch {
            state = state.copy(isLoading = true, error = null)
            val deviceId = deviceSessionStore.deviceId(accountId)
            if (deviceId == null) {
                resetThisAccount()
                return@launch
            }
            val challenge = authRepository.getChallenge(deviceId).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithBiometric(accountId, challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(deviceId, signature)
                        .onSuccess { finishAuthenticated() }
                        .onFailure {
                            state = state.copy(isLoading = false, error = "Avtentikacija ni uspela.")
                        }
                }
                .onFailure {
                    state = state.copy(isLoading = false, phase = LoginPhase.Pin)
                }
        }
    }

    fun authWithPin(pin: String) {
        scope.launch {
            state = state.copy(isLoading = true, error = null)
            val deviceId = deviceSessionStore.deviceId(accountId)
            if (deviceId == null) {
                resetThisAccount()
                return@launch
            }
            val challenge = authRepository.getChallenge(deviceId).getOrElse {
                state = state.copy(isLoading = false, error = "Napaka pri prijavi. Poskusite znova.")
                return@launch
            }
            securityRepository.signChallengeWithPin(accountId, pin, challenge)
                .onSuccess { signature ->
                    authRepository.verifySignature(deviceId, signature)
                        .onSuccess { finishAuthenticated() }
                        .onFailure {
                            state = state.copy(isLoading = false, error = "Avtentikacija ni uspela.")
                        }
                }
                .onFailure {
                    state = state.copy(isLoading = false, pin = "", error = "Napačen PIN. Poskusite znova.")
                }
        }
    }
```

- [ ] **Step 2: Update the `ResetSecureStorageClicked` handler**

In the `onEvent` block of `LoginRoute`, replace the `LoginEvent.ResetSecureStorageClicked -> { ... }` branch with:

```kotlin
                LoginEvent.ResetSecureStorageClicked -> resetThisAccount()
```

- [ ] **Step 3: Compile check**

Run: `gradlew.bat :composeApp:compileCommonMainKotlinMetadata`
Expected: FAIL at the single call site `App.kt` (`LoginRoute(...)` now needs `accountId`). That call site is updated in Task G. The `LoginRoute.kt` file itself must otherwise compile — verify no other errors by reading the compiler output (only the `App.kt` arity error is acceptable here).

> If the subagent cannot leave the build red, it may apply the minimal Task G Step-by-Step change for the `LoginRoute(...)` call only. Otherwise leave it for Task G and note the expected single error.

- [ ] **Step 4: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/feature/login/LoginRoute.kt
git commit -m "feat(mobile): account-scoped login unlock"
```

---

# WAVE 3 — sequential (after Waves 1–2)

## Task G: App.kt routing + chooser integration

**Files:**
- Modify: `composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/App.kt`

**Depends on:** C, F (and transitively A, B). Integration task — run last.

Routing rules (unauthenticated): 0 profiles → Welcome; 1 → `Login(profiles[0].id)`; 2+ → `AccountPicker`. Add-account from picker → RegistrationCode; registration back returns to picker when profiles exist, else Welcome.

- [ ] **Step 1: Add imports**

In `App.kt`, add to the import block:

```kotlin
import si.mentis.eprevzemmobile.feature.accountpicker.AccountPickerRoute
```

- [ ] **Step 2: Add destinations and depths**

In the `AppDestination` sealed interface, add:

```kotlin
    data object AccountPicker : AppDestination
    data class Login(val accountId: String) : AppDestination
```

and **remove** the old `data object Login : AppDestination`.

In the `AppDestination.depth` `when`, replace the `AppDestination.Login -> 0` line and add the picker:

```kotlin
    AppDestination.AccountPicker -> 0
    is AppDestination.Login -> 0
```

- [ ] **Step 3: Rewrite the unauthenticated routing branch**

In `LaunchedEffect(session)`, replace the `AuthSession.Unauthenticated -> ...` branch with a profiles-driven decision. Replace:

```kotlin
                AuthSession.Unauthenticated -> if (AppContainer.securityRepository.isRegistered()) {
                    AppDestination.Login
                } else {
                    AppDestination.Welcome
                }
```

with:

```kotlin
                AuthSession.Unauthenticated -> {
                    val profiles = AppContainer.sessionStore.profiles.value
                    when (profiles.size) {
                        0 -> AppDestination.Welcome
                        1 -> AppDestination.Login(profiles.first().id)
                        else -> AppDestination.AccountPicker
                    }
                }
```

Also extend the `is AuthSession.Authenticated` branch's inner `when (destination)` reset list so the new destinations route to the home target. Replace:

```kotlin
                    when (destination) {
                        AppDestination.Loading,
                        AppDestination.Welcome,
                        AppDestination.Login,
                        AppDestination.RegistrationCode,
                        is AppDestination.ConfirmAccount -> target
                        else -> destination
                    }
```

with:

```kotlin
                    when (destination) {
                        AppDestination.Loading,
                        AppDestination.Welcome,
                        AppDestination.AccountPicker,
                        is AppDestination.Login,
                        AppDestination.RegistrationCode,
                        is AppDestination.ConfirmAccount -> target
                        else -> destination
                    }
```

- [ ] **Step 4: Update the `AnimatedContent` `when (dest)` arms**

Replace the existing `AppDestination.Login -> LoginRoute(...)` arm with an account-scoped one plus the picker arm. Replace:

```kotlin
                AppDestination.Login -> LoginRoute(
                    onResetSecureStorage = {
                        destination = AppDestination.Welcome
                    },
                )
```

with:

```kotlin
                is AppDestination.Login -> LoginRoute(
                    accountId = dest.accountId,
                    onResetSecureStorage = {
                        val profiles = AppContainer.sessionStore.profiles.value
                        destination = when (profiles.size) {
                            0 -> AppDestination.Welcome
                            1 -> AppDestination.Login(profiles.first().id)
                            else -> AppDestination.AccountPicker
                        }
                    },
                )
                AppDestination.AccountPicker -> AccountPickerRoute(
                    onAccountSelected = { id -> destination = AppDestination.Login(id) },
                    onAddAccount = { destination = AppDestination.RegistrationCode },
                )
```

- [ ] **Step 5: Make registration "back" return to the picker when accounts exist**

Replace the `AppDestination.RegistrationCode -> RegistrationCodeRoute(...)` arm with:

```kotlin
                AppDestination.RegistrationCode -> RegistrationCodeRoute(
                    onBack = {
                        val profiles = AppContainer.sessionStore.profiles.value
                        destination = if (profiles.isEmpty()) {
                            AppDestination.Welcome
                        } else {
                            AppDestination.AccountPicker
                        }
                    },
                    onCodeAccepted = { code -> destination = AppDestination.ConfirmAccount(code) },
                )
```

- [ ] **Step 6: Full compile + test run**

Run:
```
gradlew.bat :composeApp:compileCommonMainKotlinMetadata
gradlew.bat :composeApp:testDebugUnitTest
```
Expected: BUILD SUCCESSFUL; all tests PASS (DeviceSessionStore, LocalSecurityRepository, PersistedSessionStore, AccountPickerMapper, HttpRegistration, plus existing suite).

- [ ] **Step 7: Commit**

```bash
git add composeApp/src/commonMain/kotlin/si/mentis/eprevzemmobile/App.kt
git commit -m "feat(mobile): account chooser routing for multiple accounts"
```

---

## Final verification (after all tasks)

- [ ] **Step 1: Clean compile + full test suite**

Run from `ePrevzemMobile/`:
```
gradlew.bat :composeApp:compileCommonMainKotlinMetadata
gradlew.bat :composeApp:testDebugUnitTest
```
Expected: BUILD SUCCESSFUL, 0 failures.

- [ ] **Step 2: Manual smoke (optional, device/emulator)**

```
gradlew.bat :composeApp:installDebug
```
Verify: register first account → home; background+reopen with one account → straight to that account's unlock; register a second account (from picker "Dodaj račun") → home; reopen with two accounts → chooser shows name + type (Zaposleni/Občan) + org name for the employee; selecting a row → that account's PIN/biometric → its home.

---

## Self-review (performed during planning)

- **Spec coverage:** per-account credentials → Tasks A, B, D, E, F; staging→commit → B, E; chooser with name/type/org → C; routing 0/1/2 → G; reset scoped per account → B, F; tests → A, B, C, G. Removal deferred per spec (no task). ✓
- **Type consistency:** `accountId`/`deviceId` parameter names, `commitRegistration`/`discardStaging`/`reset(accountId)`, `AccountRow`/`AccountType`/`toAccountRows`, `AppDestination.Login(accountId)` used consistently across tasks. ✓
- **Expect-class testability:** `SecurityCrypto`/`BiometricAuthenticator`/`SecureStorage` are `expect class`es and cannot be faked directly — Task B introduces `SecurityCryptoPort`/`BiometricGate`/`SecurityKeyStore` ports with production adapters so commonTest can substitute in-memory fakes. ✓
- **Placeholders:** none — every code step is complete. One clearly-bounded contingency is flagged with a concrete fallback (Task F's expected single arity error, resolved by Task G). ✓
