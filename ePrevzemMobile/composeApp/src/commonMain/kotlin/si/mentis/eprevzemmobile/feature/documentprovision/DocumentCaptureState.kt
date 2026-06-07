package si.mentis.eprevzemmobile.feature.documentprovision

import androidx.compose.runtime.Immutable

enum class CaptureStep { SELFIE, DOCUMENT, VERIFYING, ERROR }

@Immutable
data class DocumentCaptureState(
    val step: CaptureStep = CaptureStep.SELFIE,
    val selfieBytes: ByteArray? = null,
    val documentBytes: ByteArray? = null,
    val errorReasons: List<String> = emptyList(),
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is DocumentCaptureState) return false
        return step == other.step &&
            selfieBytes.contentEquals(other.selfieBytes ?: ByteArray(0)) &&
            documentBytes.contentEquals(other.documentBytes ?: ByteArray(0)) &&
            errorReasons == other.errorReasons
    }

    override fun hashCode(): Int {
        var result = step.hashCode()
        result = 31 * result + (selfieBytes?.contentHashCode() ?: 0)
        result = 31 * result + (documentBytes?.contentHashCode() ?: 0)
        result = 31 * result + errorReasons.hashCode()
        return result
    }

    private fun ByteArray?.contentEquals(other: ByteArray): Boolean =
        this?.contentEquals(other) ?: (other.isEmpty())
}
