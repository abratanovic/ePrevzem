package si.mentis.eprevzemmobile.core.designsystem.components.cards

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun ELocationCard(
    name: String,
    address: String,
    modifier: Modifier = Modifier,
    distance: String? = null,
    hours: String? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing
    Column(
        verticalArrangement = Arrangement.spacedBy(10.dp),
        modifier = cardModifier(modifier.fillMaxWidth()).padding(spacing.md),
    ) {
        Row(
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        ) {
            EIconChip(icon = EPrevzemIcons.Location, tint = EIconTint.Teal)
            Column(modifier = Modifier.weight(1f)) {
                Text(text = name, style = typo.cardTitle, color = colors.textPrimary)
                Text(text = address, style = typo.bodySmall, color = colors.textSecondary)
                if (distance != null) {
                    Text(
                        text = "$distance stran",
                        style = typo.caption,
                        color = colors.textMuted,
                        modifier = Modifier.padding(top = 6.dp),
                    )
                }
            }
        }
        if (hours != null) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(spacing.xs),
                modifier = Modifier
                    .fillMaxWidth()
                    .clip(EPrevzemTheme.shapes.small)
                    .background(colors.surfaceMuted)
                    .padding(horizontal = 12.dp, vertical = 10.dp),
            ) {
                Icon(
                    imageVector = EPrevzemIcons.Clock,
                    contentDescription = null,
                    tint = colors.textMuted,
                    modifier = Modifier.size(16.dp),
                )
                Text(text = hours, style = typo.bodySmall, color = colors.textSecondary)
            }
        }
    }
}
