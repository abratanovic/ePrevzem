package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asSkiaBitmap
import androidx.compose.ui.interop.UIKitView
import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.addressOf
import kotlinx.cinterop.usePinned
import org.jetbrains.skia.EncodedImageFormat
import org.jetbrains.skia.Image as SkiaImage
import platform.CoreGraphics.CGPointMake
import platform.CoreLocation.CLLocationCoordinate2DMake
import platform.Foundation.NSData
import platform.Foundation.create
import platform.MapKit.MKAnnotationProtocol
import platform.MapKit.MKAnnotationView
import platform.MapKit.MKCoordinateRegionMakeWithDistance
import platform.MapKit.MKMapView
import platform.MapKit.MKMapViewDelegateProtocol
import platform.MapKit.MKPointAnnotation
import platform.UIKit.UIImage
import platform.UIKit.UIScreen
import platform.darwin.NSObject

@OptIn(ExperimentalForeignApi::class)
@Composable
internal actual fun PlatformStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    markerIcon: ImageBitmap,
    showZoomControls: Boolean,
    modifier: Modifier,
) {
    // MapKit has no on-screen zoom buttons; pinch-zoom is always enabled, so
    // showZoomControls has no iOS-specific effect.
    val markerImage = remember(markerIcon) { markerIcon.toUIImage() }
    // The map holds its delegate weakly — remember it so it outlives composition.
    val delegate = remember(markerImage) { StationMarkerDelegate(markerImage) }

    UIKitView(
        modifier = modifier,
        factory = {
            val coordinate = CLLocationCoordinate2DMake(latitude, longitude)
            MKMapView().apply {
                setDelegate(delegate)
                setZoomEnabled(true)
                setScrollEnabled(true)
                addAnnotation(
                    MKPointAnnotation().apply {
                        setCoordinate(coordinate)
                        setTitle(label)
                    },
                )
                setRegion(
                    MKCoordinateRegionMakeWithDistance(coordinate, 1000.0, 1000.0),
                    animated = false,
                )
            }
        },
        update = { mapView ->
            val coordinate = CLLocationCoordinate2DMake(latitude, longitude)
            mapView.setRegion(
                MKCoordinateRegionMakeWithDistance(coordinate, 1000.0, 1000.0),
                animated = false,
            )
        },
    )
}

/** Supplies the custom [markerImage] for the station annotation. */
private class StationMarkerDelegate(
    private val markerImage: UIImage,
) : NSObject(), MKMapViewDelegateProtocol {
    @OptIn(ExperimentalForeignApi::class)
    override fun mapView(
        mapView: MKMapView,
        viewForAnnotation: MKAnnotationProtocol,
    ): MKAnnotationView {
        val reuseId = "station-marker"
        val view = mapView.dequeueReusableAnnotationViewWithIdentifier(reuseId)
            ?: MKAnnotationView(annotation = viewForAnnotation, reuseIdentifier = reuseId)
        view.annotation = viewForAnnotation
        view.image = markerImage
        view.centerOffset = CGPointMake(0.0, 0.0)
        return view
    }
}

/** Converts a Compose [ImageBitmap] to a screen-scaled [UIImage] via Skia PNG. */
@OptIn(ExperimentalForeignApi::class)
private fun ImageBitmap.toUIImage(): UIImage {
    val skiaImage = SkiaImage.makeFromBitmap(asSkiaBitmap())
    val pngData = skiaImage.encodeToData(EncodedImageFormat.PNG)
        ?: error("Failed to encode station marker bitmap")
    val bytes = pngData.bytes
    val nsData = bytes.usePinned { pinned ->
        NSData.create(bytes = pinned.addressOf(0), length = bytes.size.toULong())
    }
    return UIImage(data = nsData, scale = UIScreen.mainScreen.scale)
}
