package si.mentis.eprevzemmobile.data.delegation

interface DelegationRepository {
    suspend fun lookupByEmso(emso: String): Result<DelegatePerson>
    suspend fun getDelegations(pickupId: String): List<DelegationRecord>
    suspend fun addDelegation(pickupId: String, emso: String): Result<DelegationRecord>
    suspend fun removeDelegation(delegationId: String): Result<Unit>
}
