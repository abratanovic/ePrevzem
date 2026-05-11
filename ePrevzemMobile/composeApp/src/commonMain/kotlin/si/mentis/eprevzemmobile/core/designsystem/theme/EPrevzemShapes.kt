package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Shapes
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.unit.dp

@Immutable
data class EPrevzemShapes(
    val small: RoundedCornerShape = RoundedCornerShape(6.dp),
    val medium: RoundedCornerShape = RoundedCornerShape(10.dp),
    val large: RoundedCornerShape = RoundedCornerShape(14.dp),
    val extraLarge: RoundedCornerShape = RoundedCornerShape(20.dp),
    val button: RoundedCornerShape = RoundedCornerShape(12.dp),
    val pill: RoundedCornerShape = RoundedCornerShape(999.dp),
    val bottomSheet: RoundedCornerShape = RoundedCornerShape(
        topStart = 20.dp, topEnd = 20.dp, bottomStart = 0.dp, bottomEnd = 0.dp
    ),
)

internal fun EPrevzemShapes.toMaterial3Shapes(): Shapes = Shapes(
    extraSmall = small,
    small = small,
    medium = medium,
    large = large,
    extraLarge = extraLarge,
)

val LocalEPrevzemShapes = staticCompositionLocalOf { EPrevzemShapes() }
