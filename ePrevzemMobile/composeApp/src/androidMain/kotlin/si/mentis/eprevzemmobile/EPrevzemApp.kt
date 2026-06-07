package si.mentis.eprevzemmobile

import android.app.Application
import androidx.fragment.app.FragmentActivity
import org.osmdroid.config.Configuration

class EPrevzemApp : Application() {
    override fun onCreate() {
        super.onCreate()
        AndroidAppContext.application = this
        // osmdroid needs a user agent (HTTP tile requests are rejected without one)
        // and a writable cache/config path before any MapView is created.
        Configuration.getInstance().load(
            this,
            getSharedPreferences("osmdroid", MODE_PRIVATE),
        )
        Configuration.getInstance().userAgentValue = packageName
    }
}

internal object AndroidAppContext {
    @Volatile
    var application: Application? = null

    @Volatile
    var currentActivity: FragmentActivity? = null
}
