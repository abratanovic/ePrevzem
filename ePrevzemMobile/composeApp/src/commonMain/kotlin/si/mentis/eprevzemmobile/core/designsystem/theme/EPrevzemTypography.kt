package si.mentis.eprevzemmobile.core.designsystem.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import eprevzemmobile.composeapp.generated.resources.Res
import eprevzemmobile.composeapp.generated.resources.inter_variable
import org.jetbrains.compose.resources.Font

@Composable
fun appFontFamily(): FontFamily = FontFamily(
    Font(Res.font.inter_variable, FontWeight.Normal),
    Font(Res.font.inter_variable, FontWeight.Medium),
    Font(Res.font.inter_variable, FontWeight.SemiBold),
    Font(Res.font.inter_variable, FontWeight.Bold),
    Font(Res.font.inter_variable, FontWeight.ExtraBold),
)

@Composable
fun monoFontFamily(): FontFamily = FontFamily.Monospace

@Immutable
data class EPrevzemTypography(
    val display: TextStyle,
    val title: TextStyle,
    val section: TextStyle,
    val cardTitle: TextStyle,
    val body: TextStyle,
    val bodySmall: TextStyle,
    val caption: TextStyle,
    val button: TextStyle,
    val label: TextStyle,
    val mono: TextStyle,
)

@Composable
internal fun defaultEPrevzemTypography(): EPrevzemTypography {
    val sans = appFontFamily()
    val mono = monoFontFamily()
    return EPrevzemTypography(
        display = TextStyle(
            fontFamily = sans, fontSize = 32.sp, lineHeight = 38.sp,
            fontWeight = FontWeight.Bold, letterSpacing = (-0.32).sp,
        ),
        title = TextStyle(
            fontFamily = sans, fontSize = 24.sp, lineHeight = 30.sp,
            fontWeight = FontWeight.Bold, letterSpacing = (-0.24).sp,
        ),
        section = TextStyle(
            fontFamily = sans, fontSize = 18.sp, lineHeight = 24.sp,
            fontWeight = FontWeight.SemiBold,
        ),
        cardTitle = TextStyle(
            fontFamily = sans, fontSize = 16.sp, lineHeight = 22.sp,
            fontWeight = FontWeight.SemiBold,
        ),
        body = TextStyle(
            fontFamily = sans, fontSize = 15.sp, lineHeight = 22.sp,
            fontWeight = FontWeight.Normal,
        ),
        bodySmall = TextStyle(
            fontFamily = sans, fontSize = 13.sp, lineHeight = 19.sp,
            fontWeight = FontWeight.Normal,
        ),
        caption = TextStyle(
            fontFamily = sans, fontSize = 12.sp, lineHeight = 17.sp,
            fontWeight = FontWeight.Normal, letterSpacing = 0.24.sp,
        ),
        button = TextStyle(
            fontFamily = sans, fontSize = 15.sp, lineHeight = 18.sp,
            fontWeight = FontWeight.SemiBold, letterSpacing = 0.15.sp,
        ),
        label = TextStyle(
            fontFamily = sans, fontSize = 13.sp, lineHeight = 16.sp,
            fontWeight = FontWeight.SemiBold,
        ),
        mono = TextStyle(
            fontFamily = mono, fontSize = 13.sp, lineHeight = 18.sp,
            fontWeight = FontWeight.Medium,
        ),
    )
}

val LocalEPrevzemTypography = staticCompositionLocalOf<EPrevzemTypography> {
    error("EPrevzemTypography not provided. Wrap your UI in EPrevzemTheme {}.")
}
