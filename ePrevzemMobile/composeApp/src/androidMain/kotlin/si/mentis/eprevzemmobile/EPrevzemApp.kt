package si.mentis.eprevzemmobile

import android.app.Application
import androidx.fragment.app.FragmentActivity

class EPrevzemApp : Application() {
    override fun onCreate() {
        super.onCreate()
        AndroidAppContext.application = this
    }
}

internal object AndroidAppContext {
    @Volatile
    var application: Application? = null

    @Volatile
    var currentActivity: FragmentActivity? = null
}
