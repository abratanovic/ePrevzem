package si.mentis.eprevzemmobile

interface Platform {
    val name: String
}

expect fun getPlatform(): Platform