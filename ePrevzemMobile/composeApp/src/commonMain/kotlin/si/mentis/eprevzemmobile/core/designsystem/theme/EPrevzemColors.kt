package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.runtime.Immutable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color

@Immutable
data class EPrevzemColors(
    val primary: Color,
    val primaryDark: Color,
    val primaryLight: Color,
    val primary50: Color,
    val primary100: Color,

    val secondary: Color,
    val secondaryLight: Color,
    val secondary50: Color,

    val accent: Color,
    val accentLight: Color,

    val background: Color,
    val surface: Color,
    val surfaceMuted: Color,
    val surfaceSunken: Color,

    val textPrimary: Color,
    val textSecondary: Color,
    val textMuted: Color,
    val textOnPrimary: Color,
    val textLink: Color,

    val border: Color,
    val divider: Color,
    val focus: Color,

    val success: Color,
    val successBg: Color,
    val warning: Color,
    val warningBg: Color,
    val error: Color,
    val errorBg: Color,
    val info: Color,
    val infoBg: Color,

    val disabledBg: Color,
    val disabledFg: Color,
)

internal val LightEPrevzemColors = EPrevzemColors(
    primary       = Color(0xFF0F5132),
    primaryDark   = Color(0xFF0A3A24),
    primaryLight  = Color(0xFF1B7A4C),
    primary50     = Color(0xFFE8F1EC),
    primary100    = Color(0xFFC8DDD2),

    secondary       = Color(0xFF5C8A9E),
    secondaryLight  = Color(0xFFB8D4DD),
    secondary50     = Color(0xFFEAF1F4),

    accent       = Color(0xFFB8862A),
    accentLight  = Color(0xFFF1E4C2),

    background     = Color(0xFFF7F5F0),
    surface        = Color(0xFFFFFFFF),
    surfaceMuted   = Color(0xFFF0EEE8),
    surfaceSunken  = Color(0xFFECEAE3),

    textPrimary    = Color(0xFF1A2330),
    textSecondary  = Color(0xFF4A5568),
    textMuted      = Color(0xFF8A94A3),
    textOnPrimary  = Color(0xFFFFFFFF),
    textLink       = Color(0xFF0F5132),

    border    = Color(0xFFE2E0D8),
    divider   = Color(0xFFECEAE3),
    focus     = Color(0xFF1B7A4C),

    success    = Color(0xFF2E7D5B),
    successBg  = Color(0xFFE5F1EB),
    warning    = Color(0xFFC77B1F),
    warningBg  = Color(0xFFFBEFDC),
    error      = Color(0xFFB33A3A),
    errorBg    = Color(0xFFF8E5E5),
    info       = Color(0xFF2D6CB0),
    infoBg     = Color(0xFFE3ECF6),

    disabledBg = Color(0xFFE8E6DF),
    disabledFg = Color(0xFFB0ADA3),
)

val LocalEPrevzemColors = staticCompositionLocalOf { LightEPrevzemColors }
