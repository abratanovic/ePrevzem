package si.mentis.eprevzemmobile

internal actual object PlatformConfig {
    actual val eprevzemApiBaseUrl: String = BuildConfig.EPREVZEM_API_BASE_URL
    actual val cvIdentityBaseUrl: String = BuildConfig.CV_IDENTITY_BASE_URL
}
