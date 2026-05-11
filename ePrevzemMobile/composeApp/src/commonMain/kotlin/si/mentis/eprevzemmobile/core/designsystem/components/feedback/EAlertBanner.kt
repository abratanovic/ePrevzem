package si.mentis.eprevzemmobile.core.designsystem.components.feedback

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

enum class EAlertType { Warning, Error, Info }

@Composable
fun EAlertBanner(
    type: EAlertType,
    title: String,
    modifier: Modifier = Modifier,
    message: String? = null,
    icon: Painter? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    val (bg, fg) = when (type) {
        EAlertType.Warning -> colors.warningBg to colors.warning
        EAlertType.Error -> colors.errorBg to colors.error
        EAlertType.Info -> colors.infoBg to colors.info
    }
    val resolvedIcon: Painter = icon ?: when (type) {
        EAlertType.Warning -> EPrevzemIcons.warning()
        EAlertType.Error -> EPrevzemIcons.error()
        EAlertType.Info -> EPrevzemIcons.info()
    }

    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = modifier
            .fillMaxWidth()
            .clip(EPrevzemTheme.shapes.medium)
            .background(bg)
            .border(1.dp, fg.copy(alpha = 0.2f), EPrevzemTheme.shapes.medium)
            .padding(horizontal = 14.dp, vertical = 12.dp),
    ) {
        Box(contentAlignment = Alignment.Center, modifier = Modifier.size(20.dp)) {
            Icon(
                painter = resolvedIcon,
                contentDescription = null,
                tint = fg,
                modifier = Modifier.size(20.dp),
            )
        }
        Column(
            verticalArrangement = Arrangement.spacedBy(2.dp),
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text(
                text = title,
                style = typo.bodySmall.copy(fontWeight = FontWeight.SemiBold),
                color = fg,
            )
            if (message != null) {
                Text(
                    text = message,
                    style = typo.caption,
                    color = fg.copy(alpha = 0.85f),
                )
            }
        }
    }
}
