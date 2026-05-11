package si.mentis.eprevzemmobile

import si.mentis.eprevzemmobile.data.locker.Direct4MeLockerRepository
import si.mentis.eprevzemmobile.data.locker.LockerRepository
import si.mentis.eprevzemmobile.data.pickups.FakePickupRepository
import si.mentis.eprevzemmobile.data.pickups.PickupRepository
import si.mentis.eprevzemmobile.data.registration.FakeRegistrationRepository
import si.mentis.eprevzemmobile.data.registration.RegistrationRepository

object AppContainer {
    val registrationRepository: RegistrationRepository = FakeRegistrationRepository()
    val pickupRepository: PickupRepository = FakePickupRepository()
    val lockerRepository: LockerRepository = Direct4MeLockerRepository(
        apiKey = PlatformConfig.direct4MeApiKey,
    )
}
