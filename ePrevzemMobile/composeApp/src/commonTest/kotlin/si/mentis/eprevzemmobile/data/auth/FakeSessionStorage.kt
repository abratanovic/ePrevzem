package si.mentis.eprevzemmobile.data.auth

class FakeSessionStorage(initial: Map<String, String> = emptyMap()) : SessionStorage {
    private val data = initial.toMutableMap()

    fun snapshot(): Map<String, String> = data.toMap()

    fun seed(key: String, value: String) {
        data[key] = value
    }

    override suspend fun read(key: String): String? = data[key]

    override suspend fun write(key: String, value: String) {
        data[key] = value
    }

    override suspend fun remove(key: String) {
        data.remove(key)
    }
}
