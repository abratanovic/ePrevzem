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
