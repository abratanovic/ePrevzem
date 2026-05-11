package si.mentis.eprevzemmobile

import android.app.Application

class EPrevzemApp : Application() {
    override fun onCreate() {
        super.onCreate()
        AndroidAppContext.application = this
    }
}

internal object AndroidAppContext {
    @Volatile
    var application: Application? = null
}
