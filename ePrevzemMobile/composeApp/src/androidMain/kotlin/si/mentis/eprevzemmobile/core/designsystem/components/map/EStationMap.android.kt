package si.mentis.eprevzemmobile.core.designsystem.components.map

import android.graphics.drawable.BitmapDrawable
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.viewinterop.AndroidView
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.CustomZoomButtonsController
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker

@Composable
internal actual fun PlatformStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    markerIcon: ImageBitmap,
    showZoomControls: Boolean,
    modifier: Modifier,
) {
    AndroidView(
        modifier = modifier,
        factory = { context ->
            MapView(context).apply {
                setTileSource(TileSourceFactory.MAPNIK)
                setMultiTouchControls(true)
                isHorizontalMapRepetitionEnabled = false
                isVerticalMapRepetitionEnabled = false
                zoomController.setVisibility(
                    if (showZoomControls) {
                        CustomZoomButtonsController.Visibility.ALWAYS
                    } else {
                        CustomZoomButtonsController.Visibility.NEVER
                    },
                )
                val point = GeoPoint(latitude, longitude)
                controller.setZoom(16.0)
                controller.setCenter(point)
                overlays.add(
                    Marker(this).apply {
                        position = point
                        setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_CENTER)
                        icon = BitmapDrawable(context.resources, markerIcon.asAndroidBitmap())
                        title = label
                    },
                )
            }
        },
        update = { map ->
            val point = GeoPoint(latitude, longitude)
            map.controller.setCenter(point)
            map.overlays.filterIsInstance<Marker>().firstOrNull()?.apply {
                position = point
                title = label
            }
            map.invalidate()
        },
        onRelease = { map -> map.onDetach() },
    )
}
