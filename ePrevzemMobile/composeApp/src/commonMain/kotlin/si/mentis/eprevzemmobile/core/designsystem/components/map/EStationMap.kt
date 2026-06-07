package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

/**
 * Interactive native map centered on [latitude]/[longitude] with a single marker
 * at that point. Android renders an osmdroid MapView; iOS renders an Apple
 * MapKit MKMapView. The caller controls size/shape via [modifier].
 */
@Composable
expect fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier = Modifier,
)
