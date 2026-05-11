package si.mentis.eprevzemmobile.core.designsystem.components.cards

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun EInfoCard(
    icon: Painter,
    label: String? = null,
    value: String,
    modifier: Modifier = Modifier,
    tint: EIconTint = EIconTint.Green,
    trailing: @Composable (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(14.dp),
        modifier = cardModifier(modifier.fillMaxWidth()).padding(spacing.md),
    ) {
        EIconChip(painter = icon, tint = tint)
        Column(modifier = Modifier.weight(1f)) {
            if (label != null) {
                Text(text = label, style = typo.caption, color = colors.textMuted)
            }
            Text(text = value, style = typo.bodySmall, color = colors.textSecondary)
        }
        if (trailing != null) trailing()
    }
}
