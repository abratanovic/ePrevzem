package si.mentis.eprevzemmobile.core.designsystem.components.map

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Canvas
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ColorFilter
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.drawscope.CanvasDrawScope
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Raw native map: renders OSM-style tiles centered on [latitude]/[longitude]
 * with a single [markerIcon] at that point. Android uses an osmdroid MapView;
 * iOS uses an Apple MapKit MKMapView. [showZoomControls] enables the built-in
 * zoom buttons (Android only — iOS has no on-screen zoom control). Pan and
 * pinch-zoom are always available.
 */
@Composable
internal expect fun PlatformStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    markerIcon: ImageBitmap,
    showZoomControls: Boolean,
    modifier: Modifier = Modifier,
)

/**
 * Interactive station map for the design system. Shows an inline map with a
 * marker at the station; a corner expand button opens a fullscreen map with
 * full controls. The marker reproduces the app's location pin — a solid
 * [EPrevzemTheme.colors.primary] circle with the white `ic_location` glyph.
 */
@Composable
fun EStationMap(
    latitude: Double,
    longitude: Double,
    label: String,
    modifier: Modifier = Modifier,
) {
    val markerIcon = rememberStationMarker()
    var fullscreen by remember { mutableStateOf(false) }

    Box(modifier = modifier) {
        PlatformStationMap(
            latitude = latitude,
            longitude = longitude,
            label = label,
            markerIcon = markerIcon,
            showZoomControls = false,
            modifier = Modifier.fillMaxSize(),
        )
        MapCircleButton(
            icon = EPrevzemIcons.fullscreen(),
            contentDescription = "Celozaslonski prikaz",
            onClick = { fullscreen = true },
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(8.dp),
        )
    }

    if (fullscreen) {
        Dialog(
            onDismissRequest = { fullscreen = false },
            properties = DialogProperties(usePlatformDefaultWidth = false),
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(EPrevzemTheme.colors.surface),
            ) {
                PlatformStationMap(
                    latitude = latitude,
                    longitude = longitude,
                    label = label,
                    markerIcon = markerIcon,
                    showZoomControls = true,
                    modifier = Modifier.fillMaxSize(),
                )
                MapCircleButton(
                    icon = EPrevzemIcons.close(),
                    contentDescription = "Zapri",
                    onClick = { fullscreen = false },
                    modifier = Modifier
                        .align(Alignment.TopEnd)
                        .statusBarsPadding()
                        .padding(12.dp),
                )
            }
        }
    }
}

/** Small circular surface button used for the map's expand / close affordances. */
@Composable
private fun MapCircleButton(
    icon: androidx.compose.ui.graphics.painter.Painter,
    contentDescription: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(40.dp)
            .clip(CircleShape)
            .background(colors.surface)
            .border(1.dp, colors.border, CircleShape)
            .clickable(onClick = onClick),
    ) {
        Icon(
            painter = icon,
            contentDescription = contentDescription,
            tint = colors.textPrimary,
            modifier = Modifier.size(20.dp),
        )
    }
}

/**
 * Renders the station marker once into an [ImageBitmap]: a 44 dp solid primary
 * circle with the 22 dp white `ic_location` pin centered — matching the former
 * inline placeholder pin. Each platform converts this bitmap into its native
 * marker so inline and fullscreen markers are pixel-identical.
 */
@Composable
private fun rememberStationMarker(): ImageBitmap {
    val primary = EPrevzemTheme.colors.primary
    val pin = EPrevzemIcons.location()
    val density = LocalDensity.current
    val layoutDirection = LocalLayoutDirection.current
    val sizePx = with(density) { 44.dp.roundToPx() }
    val iconPx = with(density) { 22.dp.toPx() }

    return remember(primary, pin, sizePx, iconPx) {
        val bitmap = ImageBitmap(sizePx, sizePx)
        val canvas = Canvas(bitmap)
        val canvasSize = Size(sizePx.toFloat(), sizePx.toFloat())
        CanvasDrawScope().draw(density, layoutDirection, canvas, canvasSize) {
            drawCircle(
                color = primary,
                radius = sizePx / 2f,
                center = Offset(sizePx / 2f, sizePx / 2f),
            )
            val inset = (sizePx - iconPx) / 2f
            translate(inset, inset) {
                with(pin) {
                    draw(
                        size = Size(iconPx, iconPx),
                        colorFilter = ColorFilter.tint(Color.White),
                    )
                }
            }
        }
        bitmap
    }
}
