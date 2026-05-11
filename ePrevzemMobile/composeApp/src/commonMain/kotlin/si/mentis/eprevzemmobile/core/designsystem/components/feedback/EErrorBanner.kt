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

/**
 * Inline tinted alert banner — error variant. Pair with [title] + [message]
 * to surface a recoverable failure under an input (invalid code, expired
 * code, etc.). Use the page-level [EErrorState] for empty/full-screen errors.
 */
@Composable
fun EErrorBanner(
    title: String,
    modifier: Modifier = Modifier,
    message: String? = null,
    icon: Painter = EPrevzemIcons.error(),
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing
    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = modifier
            .fillMaxWidth()
            .clip(EPrevzemTheme.shapes.medium)
            .background(colors.errorBg)
            .border(1.dp, colors.error.copy(alpha = 0.2f), EPrevzemTheme.shapes.medium)
            .padding(horizontal = 14.dp, vertical = 12.dp),
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier.size(20.dp),
        ) {
            Icon(
                painter = icon,
                contentDescription = null,
                tint = colors.error,
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
                color = colors.error,
            )
            if (message != null) {
                Text(
                    text = message,
                    style = typo.caption,
                    color = colors.error.copy(alpha = 0.85f),
                )
            }
        }
    }
}
