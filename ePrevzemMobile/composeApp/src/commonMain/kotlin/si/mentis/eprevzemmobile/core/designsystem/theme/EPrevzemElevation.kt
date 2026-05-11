package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.runtime.Immutable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

@Immutable
data class EPrevzemElevation(
    val none: Dp = 0.dp,
    val card: Dp = 1.dp,
    val elevated: Dp = 4.dp,
    val modal: Dp = 12.dp,
)

val LocalEPrevzemElevation = staticCompositionLocalOf { EPrevzemElevation() }
