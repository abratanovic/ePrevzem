package si.mentis.eprevzemmobile.data.security

import kotlinx.cinterop.ObjCObjectVar
import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.alloc
import kotlinx.cinterop.memScoped
import kotlinx.cinterop.ptr
import kotlinx.coroutines.suspendCancellableCoroutine
import platform.Foundation.NSError
import platform.LocalAuthentication.LABiometryTypeNone
import platform.LocalAuthentication.LAContext
import platform.LocalAuthentication.LAPolicyDeviceOwnerAuthenticationWithBiometrics
import kotlin.coroutines.resume

@OptIn(ExperimentalForeignApi::class)
actual class BiometricAuthenticator actual constructor() {
    actual suspend fun authenticate(reason: String): Boolean = suspendCancellableCoroutine { cont ->
        val context = LAContext()
        val canEvaluate = memScoped {
            val error = alloc<ObjCObjectVar<NSError?>>()
            context.canEvaluatePolicy(LAPolicyDeviceOwnerAuthenticationWithBiometrics, error.ptr) &&
                context.biometryType != LABiometryTypeNone
        }

        if (!canEvaluate) {
            cont.resume(false)
            return@suspendCancellableCoroutine
        }

        context.evaluatePolicy(
            LAPolicyDeviceOwnerAuthenticationWithBiometrics,
            reason,
        ) { success, _ ->
            if (cont.isActive) cont.resume(success)
        }

        cont.invokeOnCancellation {
            context.invalidate()
        }
    }
}
