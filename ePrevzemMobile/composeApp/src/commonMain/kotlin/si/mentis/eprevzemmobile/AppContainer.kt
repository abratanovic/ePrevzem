package si.mentis.eprevzemmobile

import si.mentis.eprevzemmobile.data.api.ApiClient
import si.mentis.eprevzemmobile.data.auth.AuthSession
import si.mentis.eprevzemmobile.data.auth.DeviceSessionStore
import si.mentis.eprevzemmobile.data.auth.PersistedSessionStore
import si.mentis.eprevzemmobile.data.auth.SessionStore
import si.mentis.eprevzemmobile.data.delegation.DelegationRepository
import si.mentis.eprevzemmobile.data.delegation.FakeDelegationRepository
import si.mentis.eprevzemmobile.data.insertion.HttpInsertionRepository
import si.mentis.eprevzemmobile.data.insertion.InsertionRepository
import si.mentis.eprevzemmobile.data.logevent.FakeLogEventRepository
import si.mentis.eprevzemmobile.data.logevent.LogEventRepository
import si.mentis.eprevzemmobile.data.locker.HttpLockerRepository
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.pickups.HttpPickupRepository
import si.mentis.eprevzemmobile.data.pickups.PickupRepository
import si.mentis.eprevzemmobile.data.registration.HttpRegistrationRepository
import si.mentis.eprevzemmobile.data.registration.RegistrationRepository
import si.mentis.eprevzemmobile.data.security.AuthRepository
import si.mentis.eprevzemmobile.data.security.HttpAuthRepository
import si.mentis.eprevzemmobile.data.security.LocalSecurityRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository
import si.mentis.eprevzemmobile.data.settings.LocalUserSettingsRepository
import si.mentis.eprevzemmobile.data.settings.UserSettingsRepository

object AppContainer {
    val deviceSessionStore = DeviceSessionStore()
    val sessionStore: SessionStore = PersistedSessionStore()
    private val apiClient = ApiClient(
        sessionStore = deviceSessionStore,
        activeAccountId = { (sessionStore.session.value as? AuthSession.Authenticated)?.user?.id },
    )
    val registrationRepository: RegistrationRepository = HttpRegistrationRepository(apiClient, deviceSessionStore)
    val pickupRepository: PickupRepository = HttpPickupRepository(apiClient)
    val delegationRepository: DelegationRepository = FakeDelegationRepository()
    val logEventRepository: LogEventRepository = FakeLogEventRepository()
    val securityRepository: SecurityRepository = LocalSecurityRepository()
    val userSettingsRepository: UserSettingsRepository = LocalUserSettingsRepository()
    val authRepository: AuthRepository = HttpAuthRepository(apiClient, deviceSessionStore)
    val lockerRepository: LockerRepository = HttpLockerRepository(apiClient)
    val insertionRepository: InsertionRepository = HttpInsertionRepository(apiClient)
}
