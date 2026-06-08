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

enum class EPickupStatus { Ready, Expiring, PickedUp, Expired, Draft }

private data class StatusStyle(val bg: Color, val fg: Color, val icon: Painter, val label: String)

@Composable
fun EStatusChip(
    status: EPickupStatus,
    modifier: Modifier = Modifier,
    label: String? = null,
) {
    val colors = EPrevzemTheme.colors
    val style = when (status) {
        EPickupStatus.Ready -> StatusStyle(colors.successBg, colors.success, EPrevzemIcons.success(), "Pripravljen")
        EPickupStatus.Expiring -> StatusStyle(colors.warningBg, colors.warning, EPrevzemIcons.clock(), "Poteče kmalu")
        EPickupStatus.PickedUp -> StatusStyle(colors.infoBg, colors.info, EPrevzemIcons.locker(), "Prevzeto")
        EPickupStatus.Expired -> StatusStyle(colors.errorBg, colors.error, EPrevzemIcons.blocked(), "Poteklo")
        EPickupStatus.Draft -> StatusStyle(colors.surfaceMuted, colors.textSecondary, EPrevzemIcons.drafts(), "V pripravi")
    }

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        modifier = modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(style.bg)
            .border(1.dp, style.fg.copy(alpha = 0.2f), EPrevzemTheme.shapes.pill)
            .padding(horizontal = 12.dp, vertical = 6.dp),
    ) {
        Icon(
            painter = style.icon,
            contentDescription = null,
            tint = style.fg,
            modifier = Modifier.size(14.dp),
        )
        Text(
            text = label ?: style.label,
            style = EPrevzemTheme.typography.caption.copy(fontWeight = FontWeight.SemiBold),
            color = style.fg,
        )
    }
}
