package si.mentis.eprevzemmobile.data.logevent

data class LogEvent(
    val id: String,
    val occurredAt: String,
    val actorKind: LogActorKind,
    val actorCitizenUserId: String?,
    val actorEmployeeAccountId: String?,
    val actorOrganizationAdminAccountId: String?,
    val actorSystemAdminId: String?,
    val organizationId: String?,
    val action: LogAction,
    val targetKind: LogTargetKind,
    val targetId: String,
    val details: LogEventDetails?,
)
