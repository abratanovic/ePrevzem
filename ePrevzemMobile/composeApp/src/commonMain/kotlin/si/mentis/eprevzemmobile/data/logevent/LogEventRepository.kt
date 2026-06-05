package si.mentis.eprevzemmobile.data.logevent

interface LogEventRepository {
    suspend fun getLogEventsForCurrentUser(): List<LogEvent>
}
