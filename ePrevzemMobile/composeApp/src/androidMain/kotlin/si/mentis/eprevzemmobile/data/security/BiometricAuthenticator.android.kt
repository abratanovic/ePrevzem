package si.mentis.eprevzemmobile.data.security

import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import kotlinx.coroutines.suspendCancellableCoroutine
import si.mentis.eprevzemmobile.AndroidAppContext
import java.util.concurrent.Executor
import kotlin.coroutines.resume

actual class BiometricAuthenticator actual constructor() {
    actual suspend fun authenticate(reason: String): Boolean = suspendCancellableCoroutine { cont ->
        val context = AndroidAppContext.application
        val activity = AndroidAppContext.currentActivity
        if (context == null || activity == null) {
            cont.resume(false)
            return@suspendCancellableCoroutine
        }
        val authenticators = BiometricManager.Authenticators.BIOMETRIC_STRONG or
            BiometricManager.Authenticators.DEVICE_CREDENTIAL
        if (BiometricManager.from(context).canAuthenticate(authenticators) != BiometricManager.BIOMETRIC_SUCCESS) {
            cont.resume(false)
            return@suspendCancellableCoroutine
        }

        val prompt = BiometricPrompt(
            activity,
            Executor { command -> activity.runOnUiThread(command) },
            object : BiometricPrompt.AuthenticationCallback() {
                override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                    if (cont.isActive) cont.resume(true)
                }

                override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                    if (cont.isActive) cont.resume(false)
                }

                override fun onAuthenticationFailed() {
                    if (cont.isActive) cont.resume(false)
                }
            },
        )
        cont.invokeOnCancellation { prompt.cancelAuthentication() }
        prompt.authenticate(
            BiometricPrompt.PromptInfo.Builder()
                .setTitle("Biometrična potrditev")
                .setSubtitle(reason)
                .setAllowedAuthenticators(authenticators)
                .build(),
        )
    }
}
