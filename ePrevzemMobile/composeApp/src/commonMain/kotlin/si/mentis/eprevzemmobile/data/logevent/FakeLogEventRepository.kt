package si.mentis.eprevzemmobile.data.logevent

import kotlinx.coroutines.delay

class FakeLogEventRepository : LogEventRepository {

    private val logEvents = listOf(
        LogEvent(
            id = "1",
            occurredAt = "2026-05-12T14:32:00Z",
            actorKind = LogActorKind.Citizen,
            actorCitizenUserId = "citizen-1",
            actorEmployeeAccountId = null,
            actorSystemAdminId = null,
            organizationId = "org-university-ljubljana",
            action = LogAction.PackagePickedUpByCitizen,
            targetKind = LogTargetKind.Package,
            targetId = "package-diploma",
            details = LogEventDetails(
                documentTitle = "Diploma",
                organizationName = "Univerza v Ljubljani",
                lockerLabel = "Paketnik #7",
                location = "Kongresni trg, Ljubljana",
            ),
        ),
        LogEvent(
            id = "2",
            occurredAt = "2026-05-09T09:18:00Z",
            actorKind = LogActorKind.Citizen,
            actorCitizenUserId = "citizen-1",
            actorEmployeeAccountId = null,
            actorSystemAdminId = null,
            organizationId = "org-upravna-enota-ljubljana",
            action = LogAction.PackagePickedUpByCitizen,
            targetKind = LogTargetKind.Package,
            targetId = "package-id-card",
            details = LogEventDetails(
                documentTitle = "Osebna izkaznica",
                organizationName = "Upravna enota Ljubljana",
                lockerLabel = "Paketnik #3",
                location = "BTC City, Ljubljana",
            ),
        ),
    )

    override suspend fun getLogEventsForCurrentUser(): List<LogEvent> {
        delay(600)
        return logEvents
    }
}
