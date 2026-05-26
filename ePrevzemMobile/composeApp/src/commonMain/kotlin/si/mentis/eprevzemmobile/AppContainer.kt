package si.mentis.eprevzemmobile

import si.mentis.eprevzemmobile.data.delegation.DelegationRepository
import si.mentis.eprevzemmobile.data.delegation.FakeDelegationRepository
import si.mentis.eprevzemmobile.data.locker.Direct4MeLockerRepository
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.pickups.FakePickupRepository
import si.mentis.eprevzemmobile.data.pickups.PickupRepository
import si.mentis.eprevzemmobile.data.registration.FakeRegistrationRepository
import si.mentis.eprevzemmobile.data.registration.RegistrationRepository
import si.mentis.eprevzemmobile.data.security.AuthRepository
import si.mentis.eprevzemmobile.data.security.FakeAuthRepository
import si.mentis.eprevzemmobile.data.security.LocalSecurityRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository

object AppContainer {
    val registrationRepository: RegistrationRepository = FakeRegistrationRepository()
    val pickupRepository: PickupRepository = FakePickupRepository()
    val delegationRepository: DelegationRepository = FakeDelegationRepository()
    val securityRepository: SecurityRepository = LocalSecurityRepository()
    val authRepository: AuthRepository = FakeAuthRepository()
    val lockerRepository: LockerRepository = Direct4MeLockerRepository(
        apiKey = PlatformConfig.direct4MeApiKey,
    )
}
