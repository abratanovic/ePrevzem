package si.mentis.eprevzemmobile.core.camera

import kotlin.test.Test
import kotlin.test.assertEquals

class LockerQrParserTest {

    @Test
    fun extracts_locker_id_from_direct4me_url_and_strips_leading_zeroes() {
        val raw = "HTTPS://B.DIRECT4.ME/02/000537/499/138/1774444011/1/64/00/"
        assertEquals("537", parseLockerId(raw))
    }

    @Test
    fun handles_lowercase_url() {
        val raw = "https://b.direct4.me/02/000537/499/138/1774444011/1/64/00/"
        assertEquals("537", parseLockerId(raw))
    }

    @Test
    fun trims_surrounding_whitespace_before_parsing() {
        val raw = "  HTTPS://B.DIRECT4.ME/02/001024/499/138/1/1/64/00/  "
        assertEquals("1024", parseLockerId(raw))
    }

    @Test
    fun keeps_a_single_zero_when_segment_is_all_zeroes() {
        val raw = "HTTPS://B.DIRECT4.ME/02/000000/499/138/1/1/64/00/"
        assertEquals("0", parseLockerId(raw))
    }

    @Test
    fun passes_through_a_plain_locker_number() {
        assertEquals("537", parseLockerId("537"))
    }

    @Test
    fun trims_a_plain_locker_number() {
        assertEquals("537", parseLockerId("  537  "))
    }
}
