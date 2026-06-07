package si.mentis.eprevzemmobile.feature.pickups

import kotlinx.datetime.TimeZone
import kotlin.test.Test
import kotlin.test.assertEquals

class AuditLogMapperTest {

    private val ljubljana = TimeZone.of("Europe/Ljubljana")

    @Test
    fun formats_utc_timestamp_in_local_time_zone() {
        // 14:32 UTC in May is 16:32 in Ljubljana (CEST, UTC+2).
        assertEquals(
            "12. 5. 2026 ob 16:32",
            "2026-05-12T14:32:00Z".toAuditLogDisplayTime(ljubljana),
        )
    }

    @Test
    fun formats_offset_timestamp_in_local_time_zone() {
        assertEquals(
            "12. 5. 2026 ob 16:32",
            "2026-05-12T14:32:00+00:00".toAuditLogDisplayTime(ljubljana),
        )
    }

    @Test
    fun converts_across_day_boundary() {
        // 23:30 UTC on 31 Dec is 00:30 on 1 Jan in Ljubljana (CET, UTC+1).
        assertEquals(
            "1. 1. 2026 ob 00:30",
            "2025-12-31T23:30:00Z".toAuditLogDisplayTime(ljubljana),
        )
    }

    @Test
    fun returns_input_when_unparseable() {
        assertEquals("not-a-date", "not-a-date".toAuditLogDisplayTime(TimeZone.UTC))
    }
}
