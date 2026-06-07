package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.interop.UIKitView
import kotlinx.cinterop.ExperimentalForeignApi
import platform.CoreLocation.CLLocationCoordinate2DMake
import platform.MapKit.MKCoordinateRegionMakeWithDistance
import platform.MapKit.MKMapView
import platform.MapKit.MKPointAnnotation

@OptIn(ExperimentalForeignApi::class)
@Composable
actual fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier,
) {
    UIKitView(
        modifier = modifier,
        factory = {
            val coordinate = CLLocationCoordinate2DMake(latitude, longitude)
            MKMapView().apply {
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
