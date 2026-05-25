package si.mentis.eprevzemmobile.data.delegation

import kotlinx.coroutines.delay

class FakeDelegationRepository : DelegationRepository {

    private val people = mapOf(
        "0101990500006" to DelegatePerson("person-1", "Ana Kovač", "ana.kovac@gov.si"),
        "1505985500123" to DelegatePerson("person-2", "Luka Novak", "luka.novak@example.com"),
    )

    private val delegations = mutableMapOf<String, MutableList<DelegationRecord>>()

    override suspend fun lookupByEmso(emso: String): Result<DelegatePerson> {
        delay(800)
        return people[emso]?.let { Result.success(it) }
            ?: Result.failure(PersonNotFoundException())
    }

    override suspend fun getDelegations(pickupId: String): List<DelegationRecord> {
        delay(300)
        return delegations[pickupId]?.toList() ?: emptyList()
    }

    override suspend fun addDelegation(pickupId: String, emso: String): Result<DelegationRecord> {
        delay(600)
        val person = people[emso] ?: return Result.failure(PersonNotFoundException())
        val record = DelegationRecord(id = "delegation-$pickupId-$emso", person = person)
        delegations.getOrPut(pickupId) { mutableListOf() }.add(record)
        return Result.success(record)
    }

    override suspend fun removeDelegation(delegationId: String): Result<Unit> {
        delay(400)
        delegations.values.forEach { list -> list.removeAll { it.id == delegationId } }
        return Result.success(Unit)
    }
}

class PersonNotFoundException : Exception("Oseba ni bila najdena v sistemu ePrevzem.")
