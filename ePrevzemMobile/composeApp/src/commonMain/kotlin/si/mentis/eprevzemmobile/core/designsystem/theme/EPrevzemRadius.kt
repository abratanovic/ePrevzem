package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.runtime.Immutable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

@Immutable
data class EPrevzemRadius(
    val sm: Dp = 6.dp,
    val md: Dp = 10.dp,
    val lg: Dp = 14.dp,
    val xl: Dp = 20.dp,
    val button: Dp = 12.dp,
    val pill: Dp = 999.dp,
)

val LocalEPrevzemRadius = staticCompositionLocalOf { EPrevzemRadius() }
