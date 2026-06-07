package si.mentis.eprevzemmobile.data.insertion

/** Packages awaiting placement at a station, and the free lockers available there. */
data class InsertionContext(
    val stationId: String,
    val serialNumber: String,
    val locationName: String,
    val packages: List<InsertionPackage>,
    val freeLockers: List<InsertionLocker>,
)

data class InsertionPackage(
    val id: String,
    val reference: String,
    val description: String,
    val recipientName: String,
)

data class InsertionLocker(
    val lockerId: String,
    val lockerNumber: Int,
)

/** Backend operations for the operator package-insertion flow. */
interface InsertionRepository {
    /** Resolves the insertion context for a scanned station serial. */
    suspend fun getContext(serial: String): Result<InsertionContext>

    /** Persists that the package was inserted into the locker (→ InLocker). */
    suspend fun confirmInsertion(packageId: String, lockerId: String): Result<Unit>
}
