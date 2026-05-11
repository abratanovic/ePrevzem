package si.mentis.eprevzemmobile.core.designsystem.icons

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import eprevzemmobile.composeapp.generated.resources.Res
import eprevzemmobile.composeapp.generated.resources.ic_arrow_right
import eprevzemmobile.composeapp.generated.resources.ic_back
import eprevzemmobile.composeapp.generated.resources.ic_biometric
import eprevzemmobile.composeapp.generated.resources.ic_blocked
import eprevzemmobile.composeapp.generated.resources.ic_check
import eprevzemmobile.composeapp.generated.resources.ic_clock
import eprevzemmobile.composeapp.generated.resources.ic_close
import eprevzemmobile.composeapp.generated.resources.ic_docs
import eprevzemmobile.composeapp.generated.resources.ic_drafts
import eprevzemmobile.composeapp.generated.resources.ic_error
import eprevzemmobile.composeapp.generated.resources.ic_history
import eprevzemmobile.composeapp.generated.resources.ic_home
import eprevzemmobile.composeapp.generated.resources.ic_inbox
import eprevzemmobile.composeapp.generated.resources.ic_info
import eprevzemmobile.composeapp.generated.resources.ic_location
import eprevzemmobile.composeapp.generated.resources.ic_lock
import eprevzemmobile.composeapp.generated.resources.ic_locker
import eprevzemmobile.composeapp.generated.resources.ic_notifications
import eprevzemmobile.composeapp.generated.resources.ic_organization
import eprevzemmobile.composeapp.generated.resources.ic_profile
import eprevzemmobile.composeapp.generated.resources.ic_qrcode
import eprevzemmobile.composeapp.generated.resources.ic_refresh
import eprevzemmobile.composeapp.generated.resources.ic_settings
import eprevzemmobile.composeapp.generated.resources.ic_shield
import eprevzemmobile.composeapp.generated.resources.ic_success
import eprevzemmobile.composeapp.generated.resources.ic_unlock
import eprevzemmobile.composeapp.generated.resources.ic_warning
import org.jetbrains.compose.resources.painterResource
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Curated ePrevzem icon vocabulary. Every icon is backed by an SVG resource in
 * `composeResources/drawable/` and exposed as a `@Composable () -> Painter` so
 * consumers can render it with `Icon(painter = ..., ...)`.
 *
 * Adding a new icon: drop `ic_<name>.svg` into `composeResources/drawable/`,
 * import the generated `Res.drawable.ic_<name>` accessor, and add a
 * `@Composable fun <name>(): Painter` here.
 */
object EPrevzemIcons {
    @Composable fun document(): Painter = painterResource(Res.drawable.ic_docs)
    @Composable fun locker(): Painter = painterResource(Res.drawable.ic_locker)
    @Composable fun location(): Painter = painterResource(Res.drawable.ic_location)
    @Composable fun shield(): Painter = painterResource(Res.drawable.ic_shield)
    @Composable fun shieldOutlined(): Painter = painterResource(Res.drawable.ic_shield)
    @Composable fun biometric(): Painter = painterResource(Res.drawable.ic_biometric)
    @Composable fun clock(): Painter = painterResource(Res.drawable.ic_clock)
    @Composable fun organization(): Painter = painterResource(Res.drawable.ic_organization)
    @Composable fun qrCode(): Painter = painterResource(Res.drawable.ic_qrcode)
    @Composable fun notifications(): Painter = painterResource(Res.drawable.ic_notifications)
    @Composable fun profile(): Painter = painterResource(Res.drawable.ic_profile)
    @Composable fun success(): Painter = painterResource(Res.drawable.ic_success)
    @Composable fun error(): Painter = painterResource(Res.drawable.ic_error)
    @Composable fun warning(): Painter = painterResource(Res.drawable.ic_warning)
    @Composable fun history(): Painter = painterResource(Res.drawable.ic_history)
    @Composable fun unlock(): Painter = painterResource(Res.drawable.ic_unlock)
    @Composable fun check(): Painter = painterResource(Res.drawable.ic_check)
    @Composable fun info(): Painter = painterResource(Res.drawable.ic_info)
    @Composable fun home(): Painter = painterResource(Res.drawable.ic_home)
    @Composable fun back(): Painter = painterResource(Res.drawable.ic_back)
    @Composable fun close(): Painter = painterResource(Res.drawable.ic_close)
    @Composable fun settings(): Painter = painterResource(Res.drawable.ic_settings)
    @Composable fun refresh(): Painter = painterResource(Res.drawable.ic_refresh)
    @Composable fun inbox(): Painter = painterResource(Res.drawable.ic_inbox)
    @Composable fun blocked(): Painter = painterResource(Res.drawable.ic_blocked)
    @Composable fun drafts(): Painter = painterResource(Res.drawable.ic_drafts)
    @Composable fun lock(): Painter = painterResource(Res.drawable.ic_lock)
    // No dedicated ic_help.svg — re-use ic_info for now.
    @Composable fun help(): Painter = painterResource(Res.drawable.ic_info)
    @Composable fun arrowRight(): Painter = painterResource(Res.drawable.ic_arrow_right)
}

/** Standard icon sizes from the design system (16 / 20 / 24 / 36). */
object EPrevzemIconSize {
    val Small: Dp = 16.dp
    val Medium: Dp = 20.dp
    val Large: Dp = 24.dp
    val ExtraLarge: Dp = 36.dp
}

/** Default content colour for inert icons; matches `--color-text-secondary`. */
@Composable
@ReadOnlyComposable
fun defaultIconColor(): Color = EPrevzemTheme.colors.textSecondary
