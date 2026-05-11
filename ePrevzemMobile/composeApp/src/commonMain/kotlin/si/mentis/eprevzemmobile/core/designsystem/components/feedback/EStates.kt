package si.mentis.eprevzemmobile.core.designsystem.components.feedback

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun EEmptyState(
    title: String,
    modifier: Modifier = Modifier,
    icon: Painter = EPrevzemIcons.inbox(),
    message: String? = null,
    actionLabel: String? = null,
    onAction: (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    StateLayout(modifier = modifier) {
        StateIconBubble(icon = icon, bg = colors.surfaceMuted, fg = colors.textMuted)
        Text(text = title, style = EPrevzemTheme.typography.section, color = colors.textPrimary, textAlign = TextAlign.Center)
        if (message != null) {
            Text(
                text = message,
                style = EPrevzemTheme.typography.bodySmall,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
        }
        if (actionLabel != null && onAction != null) {
            ETextButton(label = actionLabel, onClick = onAction)
        }
    }
}

@Composable
fun EErrorState(
    modifier: Modifier = Modifier,
    title: String = "Prišlo je do napake",
    message: String? = null,
    retryLabel: String = "Poskusite znova",
    onRetry: (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    StateLayout(modifier = modifier) {
        StateIconBubble(icon = EPrevzemIcons.error(), bg = colors.errorBg, fg = colors.error)
        Text(text = title, style = EPrevzemTheme.typography.section, color = colors.textPrimary, textAlign = TextAlign.Center)
        if (message != null) {
            Text(
                text = message,
                style = EPrevzemTheme.typography.bodySmall,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
        }
        if (onRetry != null) {
            Box(modifier = Modifier.width(220.dp)) {
                ESecondaryButton(label = retryLabel, icon = EPrevzemIcons.refresh(), onClick = onRetry, modifier = Modifier.fillMaxWidth())
            }
        }
    }
}

@Composable
fun ELoadingState(
    modifier: Modifier = Modifier,
    message: String = "Nalaganje…",
) {
    val colors = EPrevzemTheme.colors
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(14.dp),
        modifier = modifier.fillMaxWidth().padding(horizontal = 24.dp, vertical = 40.dp),
    ) {
        CircularProgressIndicator(
            color = colors.primary,
            modifier = Modifier.size(36.dp),
            strokeWidth = 3.dp,
        )
        Text(text = message, style = EPrevzemTheme.typography.bodySmall, color = colors.textSecondary)
    }
}

@Composable
private fun StateLayout(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp),
        modifier = modifier.fillMaxWidth().padding(horizontal = 24.dp, vertical = 40.dp),
    ) {
        content()
    }
}

@Composable
private fun StateIconBubble(icon: Painter, bg: Color, fg: Color) {
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier.size(72.dp).clip(CircleShape).background(bg),
    ) {
        Icon(painter = icon, contentDescription = null, tint = fg, modifier = Modifier.size(36.dp))
    }
}
