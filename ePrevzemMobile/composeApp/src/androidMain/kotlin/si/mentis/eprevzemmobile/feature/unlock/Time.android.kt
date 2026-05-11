package si.mentis.eprevzemmobile.feature.unlock

import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

internal actual fun nowHhMm(): String =
    SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date())
