package si.mentis.eprevzemmobile.data.logevent.dto

import kotlinx.serialization.Serializable
import si.mentis.eprevzemmobile.data.logevent.LogAction
import si.mentis.eprevzemmobile.data.logevent.LogActorKind
import si.mentis.eprevzemmobile.data.logevent.LogEvent
import si.mentis.eprevzemmobile.data.logevent.LogEventDetails
import si.mentis.eprevzemmobile.data.logevent.LogTargetKind

/** Wire model for the backend `AuditLogEntryResponse`. */
@Serializable
data class AuditLogEntryDto(
    val id: String,
    val occurredAt: String,
    val actorKind: String,
    val actorCitizenUserId: String? = null,
    val actorEmployeeAccountId: String? = null,
    val actorOrganizationAdminAccountId: String? = null,
    val actorSystemAdminId: String? = null,
    val organizationId: String? = null,
    val action: String,
    val targetKind: String,
    val targetId: String,
    val details: AuditLogDetailsDto? = null,
)

@Serializable
data class AuditLogDetailsDto(
    val documentTitle: String? = null,
    val organizationName: String? = null,
    val lockerLabel: String? = null,
    val location: String? = null,
)

/**
 * Maps a wire entry to the domain [LogEvent]. Returns `null` when the actor/action/target
 * enums are not recognised, so a newer backend value never crashes an older client; such
 * entries are dropped by the caller.
 */
fun AuditLogEntryDto.toDomain(): LogEvent? {
    val actorKind = enumOrNull<LogActorKind>(actorKind) ?: return null
    val action = enumOrNull<LogAction>(action) ?: return null
    val targetKind = enumOrNull<LogTargetKind>(targetKind) ?: return null

    return LogEvent(
        id = id,
        occurredAt = occurredAt,
        actorKind = actorKind,
        actorCitizenUserId = actorCitizenUserId,
        actorEmployeeAccountId = actorEmployeeAccountId,
        actorOrganizationAdminAccountId = actorOrganizationAdminAccountId,
        actorSystemAdminId = actorSystemAdminId,
        organizationId = organizationId,
        action = action,
        targetKind = targetKind,
        targetId = targetId,
        details = details?.toDomain(),
    )
}

private fun AuditLogDetailsDto.toDomain(): LogEventDetails =
    LogEventDetails(
        documentTitle = documentTitle,
        organizationName = organizationName,
        lockerLabel = lockerLabel,
        location = location,
    )

private inline fun <reified T : Enum<T>> enumOrNull(name: String): T? =
    enumValues<T>().firstOrNull { it.name == name }
