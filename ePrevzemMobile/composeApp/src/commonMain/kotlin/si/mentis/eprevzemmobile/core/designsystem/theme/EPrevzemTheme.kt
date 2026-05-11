package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider

@Composable
fun EPrevzemTheme(
    content: @Composable () -> Unit,
) {
    val colors = LightEPrevzemColors
    val typography = defaultEPrevzemTypography()
    val spacing = EPrevzemSpacing()
    val radius = EPrevzemRadius()
    val shapes = EPrevzemShapes()
    val elevation = EPrevzemElevation()

    CompositionLocalProvider(
        LocalEPrevzemColors provides colors,
        LocalEPrevzemTypography provides typography,
        LocalEPrevzemSpacing provides spacing,
        LocalEPrevzemRadius provides radius,
        LocalEPrevzemShapes provides shapes,
        LocalEPrevzemElevation provides elevation,
    ) {
        MaterialTheme(
            colorScheme = colors.toMaterial3ColorScheme(),
            typography = typography.toMaterial3Typography(),
            shapes = shapes.toMaterial3Shapes(),
            content = content,
        )
    }
}

private fun EPrevzemColors.toMaterial3ColorScheme(): ColorScheme = lightColorScheme(
    primary = primary,
    onPrimary = textOnPrimary,
    primaryContainer = primary50,
    onPrimaryContainer = primaryDark,
    secondary = secondary,
    onSecondary = textOnPrimary,
    secondaryContainer = secondary50,
    onSecondaryContainer = secondary,
    tertiary = accent,
    onTertiary = textOnPrimary,
    tertiaryContainer = accentLight,
    onTertiaryContainer = accent,
    background = background,
    onBackground = textPrimary,
    surface = surface,
    onSurface = textPrimary,
    surfaceVariant = surfaceMuted,
    onSurfaceVariant = textSecondary,
    outline = border,
    outlineVariant = divider,
    error = error,
    onError = textOnPrimary,
    errorContainer = errorBg,
    onErrorContainer = error,
)

private fun EPrevzemTypography.toMaterial3Typography(): Typography = Typography(
    displayLarge = display,
    displayMedium = display,
    displaySmall = title,
    headlineLarge = title,
    headlineMedium = title,
    headlineSmall = section,
    titleLarge = section,
    titleMedium = cardTitle,
    titleSmall = label,
    bodyLarge = body,
    bodyMedium = body,
    bodySmall = bodySmall,
    labelLarge = button,
    labelMedium = label,
    labelSmall = caption,
)
