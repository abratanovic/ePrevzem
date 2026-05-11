package si.mentis.eprevzemmobile.feature.unlock

import platform.Foundation.NSDate
import platform.Foundation.NSDateFormatter

internal actual fun nowHhMm(): String {
    val fmt = NSDateFormatter().apply { dateFormat = "HH:mm" }
    return fmt.stringFromDate(NSDate())
}
