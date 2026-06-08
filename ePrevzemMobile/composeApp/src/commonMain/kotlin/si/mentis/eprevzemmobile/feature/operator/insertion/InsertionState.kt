package si.mentis.eprevzemmobile.feature.operator.insertion

import androidx.compose.runtime.Immutable
import si.mentis.eprevzemmobile.data.insertion.InsertionContext
import si.mentis.eprevzemmobile.data.insertion.InsertionLocker
import si.mentis.eprevzemmobile.data.insertion.InsertionPackage

/** Steps of the operator insertion flow, in order. */
enum class InsertionStep { Scan, Loading, Select, Opening, Opened, Confirming, Done }

@Immutable
data class InsertionState(
    val step: InsertionStep = InsertionStep.Scan,
    val context: InsertionContext? = null,
    val selectedPackageId: String? = null,
    val selectedLockerId: String? = null,
    val error: String? = null,
) {
    val selectedPackage: InsertionPackage?
        get() = context?.packages?.firstOrNull { it.id == selectedPackageId }
    val selectedLocker: InsertionLocker?
        get() = context?.freeLockers?.firstOrNull { it.lockerId == selectedLockerId }
    val canOpen: Boolean
        get() = selectedPackageId != null && selectedLockerId != null
}

sealed interface InsertionEvent {
    data class StationScanned(val serial: String) : InsertionEvent
    data class PackageSelected(val id: String) : InsertionEvent
    data class LockerSelected(val id: String) : InsertionEvent
    data object OpenClicked : InsertionEvent
    data object ConfirmClosedClicked : InsertionEvent
    data object Retry : InsertionEvent
    data object Back : InsertionEvent
    data object Done : InsertionEvent
}
