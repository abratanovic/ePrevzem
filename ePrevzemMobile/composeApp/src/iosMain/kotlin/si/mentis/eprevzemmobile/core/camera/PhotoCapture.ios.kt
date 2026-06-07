package si.mentis.eprevzemmobile.core.camera

import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.readBytes
import platform.UIKit.UIApplication
import platform.UIKit.UIImage
import platform.UIKit.UIImageJPEGRepresentation
import platform.UIKit.UIImagePickerController
import platform.UIKit.UIImagePickerControllerDelegateProtocol
import platform.UIKit.UIImagePickerControllerOriginalImage
import platform.UIKit.UIImagePickerControllerSourceType
import platform.UIKit.UINavigationControllerDelegateProtocol
import platform.darwin.NSObject

@Composable
actual fun rememberPhotoCaptureLauncher(onCapture: (ByteArray?) -> Unit): PhotoCaptureLauncher =
    remember { IosPhotoCaptureLauncher(onCapture) }

@OptIn(ExperimentalForeignApi::class)
private class IosPhotoCaptureLauncher(
    private val onCapture: (ByteArray?) -> Unit,
) : PhotoCaptureLauncher {

    // Strong reference prevents GC collecting the delegate while the picker is presented.
    private var activeDelegate: ImagePickerDelegate? = null

    @Suppress("DEPRECATION")
    override fun launch() {
        val rootVC = UIApplication.sharedApplication.keyWindow?.rootViewController ?: return

        val cameraType = UIImagePickerControllerSourceType.UIImagePickerControllerSourceTypeCamera
        if (!UIImagePickerController.isSourceTypeAvailable(cameraType)) {
            // Simulator or device without camera
            onCapture(null)
            return
        }

        val picker = UIImagePickerController()
        picker.sourceType = cameraType
        picker.allowsEditing = false

        val delegate = ImagePickerDelegate(
            onCapture = { bytes ->
                activeDelegate = null
                onCapture(bytes)
            },
            dismiss = { picker.dismissViewControllerAnimated(true, completion = null) },
        )
        activeDelegate = delegate
        picker.delegate = delegate

        rootVC.presentViewController(picker, animated = true, completion = null)
    }
}

@OptIn(ExperimentalForeignApi::class)
private class ImagePickerDelegate(
    private val onCapture: (ByteArray?) -> Unit,
    private val dismiss: () -> Unit,
) : NSObject(), UIImagePickerControllerDelegateProtocol, UINavigationControllerDelegateProtocol {

    override fun imagePickerController(
        picker: UIImagePickerController,
        didFinishPickingMediaWithInfo: Map<Any?, *>,
    ) {
        val image = didFinishPickingMediaWithInfo[UIImagePickerControllerOriginalImage] as? UIImage
        dismiss()
        val bytes = image?.let { img ->
            UIImageJPEGRepresentation(img, 0.85)?.let { data ->
                data.bytes?.readBytes(data.length.toInt())
            }
        }
        onCapture(bytes)
    }

    override fun imagePickerControllerDidCancel(picker: UIImagePickerController) {
        dismiss()
        onCapture(null)
    }
}
