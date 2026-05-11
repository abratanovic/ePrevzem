package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable

/**
 * Single entry point for design tokens.
 *
 * Components must read tokens via `EPrevzemTheme.colors`, `EPrevzemTheme.typography`, etc.
 * Never hardcode colours, font sizes, spacing or radii inside individual components.
 */
object EPrevzemTheme {
    val colors: EPrevzemColors
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemColors.current

    val typography: EPrevzemTypography
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemTypography.current

    val spacing: EPrevzemSpacing
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemSpacing.current

    val radius: EPrevzemRadius
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemRadius.current

    val shapes: EPrevzemShapes
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemShapes.current

    val elevation: EPrevzemElevation
        @Composable @ReadOnlyComposable
        get() = LocalEPrevzemElevation.current
}
