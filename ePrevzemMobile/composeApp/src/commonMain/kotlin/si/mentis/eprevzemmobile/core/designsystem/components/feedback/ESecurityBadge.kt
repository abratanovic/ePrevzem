package si.mentis.eprevzemmobile.core.designsystem.components.feedback

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

enum class ESecurityLevel { High, Medium, Low }

private data class BadgeStyle(val bg: Color, val fg: Color, val icon: Painter, val label: String)

@Composable
fun ESecurityBadge(
    level: ESecurityLevel,
    modifier: Modifier = Modifier,
    label: String? = null,
) {
    val colors = EPrevzemTheme.colors
    val style = when (level) {
        ESecurityLevel.High -> BadgeStyle(colors.primary50, colors.primary, EPrevzemIcons.shield(), "Visoka raven")
        ESecurityLevel.Medium -> BadgeStyle(colors.secondary50, colors.secondary, EPrevzemIcons.shieldOutlined(), "Srednja raven")
        ESecurityLevel.Low -> BadgeStyle(colors.surfaceMuted, colors.textSecondary, EPrevzemIcons.lock(), "Osnovna raven")
    }

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        modifier = modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(style.bg)
            .border(1.dp, style.fg.copy(alpha = 0.2f), EPrevzemTheme.shapes.pill)
            .padding(horizontal = 14.dp, vertical = 8.dp),
    ) {
        Icon(painter = style.icon, contentDescription = null, tint = style.fg, modifier = Modifier.size(16.dp))
        Text(
            text = label ?: style.label,
            style = EPrevzemTheme.typography.bodySmall.copy(fontWeight = FontWeight.SemiBold),
            color = style.fg,
        )
    }
}
