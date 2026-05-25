package si.mentis.eprevzemmobile

import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.Composable
import androidx.compose.ui.tooling.preview.Preview
import androidx.fragment.app.FragmentActivity

class MainActivity : FragmentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        AndroidAppContext.currentActivity = this

        setContent {
            App()
        }
    }

    override fun onResume() {
        super.onResume()
        AndroidAppContext.currentActivity = this
    }

    override fun onDestroy() {
        if (AndroidAppContext.currentActivity === this) {
            AndroidAppContext.currentActivity = null
        }
        super.onDestroy()
    }
}

@Preview
@Composable
fun AppAndroidPreview() {
    App()
}
