package si.mentis.eprevzemmobile.core.designsystem.components.cards

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

enum class EIconTint { Green, Teal, Gold, Gray }

@Composable
internal fun cardModifier(modifier: Modifier = Modifier): Modifier {
    val colors = EPrevzemTheme.colors
    val shape = EPrevzemTheme.shapes.large
    return modifier
        .clip(shape)
        .background(colors.surface)
        .border(1.dp, colors.border, shape)
}

@Composable
internal fun EIconChip(
    painter: Painter,
    tint: EIconTint = EIconTint.Green,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val (bg, fg) = when (tint) {
        EIconTint.Green -> colors.primary50 to colors.primary
        EIconTint.Teal -> colors.secondary50 to colors.secondary
        EIconTint.Gold -> colors.accentLight to colors.accent
        EIconTint.Gray -> colors.surfaceMuted to colors.textSecondary
    }
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(44.dp)
            .clip(EPrevzemTheme.shapes.medium)
            .background(bg),
    ) {
        Icon(painter = painter, contentDescription = null, tint = fg, modifier = Modifier.size(22.dp))
    }
}
