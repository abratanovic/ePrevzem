package si.mentis.eprevzemmobile.data.locker.dto

import kotlinx.serialization.Serializable

@Serializable
internal data class OpenBoxRequest(
    val boxId: Long,
    val tokenFormat: Int,
)

@Serializable
internal data class OpenBoxResponse(
    val data: String? = null,
    val result: Int = 0,
    val errorNumber: Int = 0,
)
