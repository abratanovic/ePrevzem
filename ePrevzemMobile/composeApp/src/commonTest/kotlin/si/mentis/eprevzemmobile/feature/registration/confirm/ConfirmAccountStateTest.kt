package si.mentis.eprevzemmobile.feature.registration.confirm

import kotlin.test.Test
import kotlin.test.assertEquals

class ConfirmAccountStateTest {

    @Test
    fun identity_uses_email_when_emso_is_missing() {
        val account = ConfirmAccountData(email = "janez@example.com")

        assertEquals("E-pošta", account.identityLabel)
        assertEquals("janez@example.com", account.identityValue)
    }

    @Test
    fun identity_uses_emso_when_present() {
        val account = ConfirmAccountData(
            email = "janez@example.com",
            emso = "0101000500001",
        )

        assertEquals("EMŠO", account.identityLabel)
        assertEquals("0101000500001", account.identityValue)
    }
}
