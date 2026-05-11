package si.mentis.eprevzemmobile.core.designsystem.icons

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.PathFillType
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.path
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Curated ePrevzem icon vocabulary. Maps the design system's semantic names
 * (see ICONOGRAPHY.md) to concrete [ImageVector]s.
 *
 * NOTE on the icon set: the design system specifies Material Symbols Rounded.
 * Compose Multiplatform 1.10.x does not currently publish a KMP-compatible
 * `material-icons-*` artifact, so we vendor a small set of vector literals
 * here. Real production icons can be dropped in by replacing each `by lazy`
 * body below — every component routes through `EPrevzemIcons.*`, so swapping
 * the source is a one-file change.
 */
object EPrevzemIcons {
    val Document: ImageVector by lazy { documentVector() }
    val Locker: ImageVector by lazy { lockerVector() }
    val Location: ImageVector by lazy { locationVector() }
    val Shield: ImageVector by lazy { shieldVector() }
    val ShieldOutlined: ImageVector by lazy { shieldVector() }
    val Biometric: ImageVector by lazy { fingerprintVector() }
    val Clock: ImageVector by lazy { clockVector() }
    val Organization: ImageVector by lazy { apartmentVector() }
    val QrCode: ImageVector by lazy { qrCodeVector() }
    val Notifications: ImageVector by lazy { notificationsVector() }
    val Profile: ImageVector by lazy { accountCircleVector() }
    val Success: ImageVector by lazy { checkCircleVector() }
    val Error: ImageVector by lazy { errorVector() }
    val Warning: ImageVector by lazy { warningVector() }
    val History: ImageVector by lazy { historyVector() }
    val Unlock: ImageVector by lazy { lockOpenVector() }
    val Lock: ImageVector by lazy { lockVector() }
    val Delegate: ImageVector by lazy { personAddVector() }
    val Home: ImageVector by lazy { homeVector() }
    val Back: ImageVector by lazy { arrowBackVector() }
    val Close: ImageVector by lazy { closeVector() }
    val Settings: ImageVector by lazy { settingsVector() }
    val Refresh: ImageVector by lazy { refreshVector() }
    val Help: ImageVector by lazy { helpVector() }
    val Info: ImageVector by lazy { infoVector() }
    val Inbox: ImageVector by lazy { inboxVector() }
    val Blocked: ImageVector by lazy { blockVector() }
    val Drafts: ImageVector by lazy { draftsVector() }
    val Check: ImageVector by lazy { checkVector() }
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

// ----------------------------------------------------------------------
// Inline ImageVector definitions. All paths are 24x24 Material-style SVGs.
// They are intentionally simple so they ship without any external icon
// dependency. Replace freely.
// ----------------------------------------------------------------------

private fun materialVector(name: String, pathData: String): ImageVector {
    val builder = ImageVector.Builder(
        name = name,
        defaultWidth = 24.dp,
        defaultHeight = 24.dp,
        viewportWidth = 24f,
        viewportHeight = 24f,
    )
    builder.path(
        fill = SolidColor(Color.Black),
        stroke = null,
        strokeLineWidth = 0f,
        strokeLineCap = StrokeCap.Butt,
        strokeLineJoin = StrokeJoin.Miter,
        strokeLineMiter = 4f,
        pathFillType = PathFillType.NonZero,
    ) {
        parsePath(pathData)
    }
    return builder.build()
}

private fun androidx.compose.ui.graphics.vector.PathBuilder.parsePath(d: String) {
    var i = 0
    var cmd = ' '
    val s = d
    fun skipWs() {
        while (i < s.length && (s[i] == ' ' || s[i] == ',')) i++
    }
    fun readNum(): Float {
        skipWs()
        val start = i
        if (i < s.length && (s[i] == '-' || s[i] == '+')) i++
        while (i < s.length && (s[i].isDigit() || s[i] == '.')) i++
        if (i < s.length && (s[i] == 'e' || s[i] == 'E')) {
            i++
            if (i < s.length && (s[i] == '-' || s[i] == '+')) i++
            while (i < s.length && s[i].isDigit()) i++
        }
        return s.substring(start, i).toFloat()
    }
    while (i < s.length) {
        skipWs()
        if (i >= s.length) break
        val c = s[i]
        if (c.isLetter()) {
            cmd = c
            i++
            skipWs()
        }
        when (cmd) {
            'M' -> moveTo(readNum(), readNum())
            'm' -> moveToRelative(readNum(), readNum())
            'L' -> lineTo(readNum(), readNum())
            'l' -> lineToRelative(readNum(), readNum())
            'H' -> horizontalLineTo(readNum())
            'h' -> horizontalLineToRelative(readNum())
            'V' -> verticalLineTo(readNum())
            'v' -> verticalLineToRelative(readNum())
            'C' -> curveTo(readNum(), readNum(), readNum(), readNum(), readNum(), readNum())
            'c' -> curveToRelative(readNum(), readNum(), readNum(), readNum(), readNum(), readNum())
            'S' -> reflectiveCurveTo(readNum(), readNum(), readNum(), readNum())
            's' -> reflectiveCurveToRelative(readNum(), readNum(), readNum(), readNum())
            'Q' -> quadTo(readNum(), readNum(), readNum(), readNum())
            'q' -> quadToRelative(readNum(), readNum(), readNum(), readNum())
            'Z', 'z' -> close()
            else -> i++
        }
    }
}

// Material-source SVG path data, MIT licence, 24x24 viewport.
private fun documentVector() = materialVector(
    "Document",
    "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11z"
)
private fun lockerVector() = materialVector(
    "Locker",
    "M20 2H4c-1.1 0-2 .9-2 2v3c0 .8.4 1.5 1 1.9V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.9c.6-.4 1-1.1 1-1.9V4c0-1.1-.9-2-2-2zM15 14H9v-2h6v2zM20 7H4V4h16v3z"
)
private fun locationVector() = materialVector(
    "Location",
    "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"
)
private fun shieldVector() = materialVector(
    "Shield",
    "M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm-2 16l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z"
)
private fun fingerprintVector() = materialVector(
    "Fingerprint",
    "M17.81 4.47c-.08 0-.16-.02-.23-.06C15.66 3.42 14 3 12.01 3c-1.98 0-3.86.47-5.57 1.41-.24.13-.54.04-.68-.2-.13-.24-.04-.55.2-.68C7.82 2.52 9.86 2 12.01 2c2.13 0 3.99.47 6.03 1.52.25.13.34.43.21.67-.09.18-.26.28-.44.28zM3.5 9.72c-.1 0-.2-.03-.29-.09-.23-.16-.28-.47-.12-.7.99-1.4 2.25-2.5 3.75-3.27C9.98 4.04 14 4.03 17.15 5.65c1.5.77 2.76 1.86 3.75 3.25.16.22.11.54-.12.7-.23.16-.54.11-.7-.12-.9-1.26-2.04-2.25-3.39-2.94-2.87-1.47-6.54-1.47-9.4.01-1.36.7-2.5 1.7-3.4 2.96-.08.14-.23.21-.39.21z"
)
private fun clockVector() = materialVector(
    "Clock",
    "M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"
)
private fun apartmentVector() = materialVector(
    "Apartment",
    "M17 11V3H7v4H3v14h8v-4h2v4h8V11h-4zM7 19H5v-2h2v2zm0-4H5v-2h2v2zm0-4H5V9h2v2zm4 4H9v-2h2v2zm0-4H9V9h2v2zm0-4H9V5h2v2zm4 8h-2v-2h2v2zm0-4h-2V9h2v2zm0-4h-2V5h2v2zm4 12h-2v-2h2v2zm0-4h-2v-2h2v2z"
)
private fun qrCodeVector() = materialVector(
    "QrCode",
    "M3 11h8V3H3v8zm2-6h4v4H5V5zM3 21h8v-8H3v8zm2-6h4v4H5v-4zM13 3v8h8V3h-8zm6 6h-4V5h4v4zM21 19h-2v2h2v-2zM15 13h-2v2h2v-2zM17 15h-2v2h2v-2zM15 17h-2v2h2v-2zM17 19h-2v2h2v-2zM19 17h-2v2h2v-2zM19 13h-2v2h2v-2zM21 15h-2v2h2v-2z"
)
private fun notificationsVector() = materialVector(
    "Notifications",
    "M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"
)
private fun accountCircleVector() = materialVector(
    "AccountCircle",
    "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"
)
private fun checkCircleVector() = materialVector(
    "CheckCircle",
    "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
)
private fun errorVector() = materialVector(
    "Error",
    "M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"
)
private fun warningVector() = materialVector(
    "Warning",
    "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"
)
private fun historyVector() = materialVector(
    "History",
    "M13 3c-4.97 0-9 4.03-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42C8.27 19.99 10.51 21 13 21c4.97 0 9-4.03 9-9s-4.03-9-9-9zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8z"
)
private fun lockOpenVector() = materialVector(
    "LockOpen",
    "M12 17c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm6-9h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6h1.9c0-1.71 1.39-3.1 3.1-3.1s3.1 1.39 3.1 3.1v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm0 12H6V10h12v10z"
)
private fun lockVector() = materialVector(
    "Lock",
    "M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z"
)
private fun personAddVector() = materialVector(
    "PersonAdd",
    "M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"
)
private fun homeVector() = materialVector(
    "Home",
    "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"
)
private fun arrowBackVector() = materialVector(
    "ArrowBack",
    "M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20z"
)
private fun closeVector() = materialVector(
    "Close",
    "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
)
private fun settingsVector() = materialVector(
    "Settings",
    "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94 0 .31.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"
)
private fun refreshVector() = materialVector(
    "Refresh",
    "M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"
)
private fun helpVector() = materialVector(
    "Help",
    "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z"
)
private fun infoVector() = materialVector(
    "Info",
    "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"
)
private fun inboxVector() = materialVector(
    "Inbox",
    "M19 3H5c-1.11 0-2 .89-2 2v14c0 1.11.89 2 2 2h14c1.11 0 2-.89 2-2V5c0-1.11-.89-2-2-2zm0 12h-4c0 1.66-1.35 3-3 3s-3-1.34-3-3H5V5h14v10z"
)
private fun blockVector() = materialVector(
    "Block",
    "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM4 12c0-4.42 3.58-8 8-8 1.85 0 3.55.63 4.9 1.69L5.69 16.9C4.63 15.55 4 13.85 4 12zm8 8c-1.85 0-3.55-.63-4.9-1.69L18.31 7.1C19.37 8.45 20 10.15 20 12c0 4.42-3.58 8-8 8z"
)
private fun draftsVector() = materialVector(
    "Drafts",
    "M21.99 8c0-.72-.37-1.35-.94-1.7L12 1 2.95 6.3C2.38 6.65 2 7.28 2 8v10c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2l-.01-10zM12 13L3.74 7.84 12 3l8.26 4.84L12 13z"
)
private fun checkVector() = materialVector(
    "Check",
    "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"
)
