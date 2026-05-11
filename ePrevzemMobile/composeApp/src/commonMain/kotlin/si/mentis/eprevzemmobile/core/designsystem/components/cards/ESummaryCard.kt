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
import androidx.compose.ui.graphics.vector.ImageVector
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun ESummaryCard(
    title: String,
    icon: ImageVector,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    tint: EIconTint = EIconTint.Green,
    content: @Composable (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing
    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = cardModifier(modifier.fillMaxWidth()).padding(spacing.md),
    ) {
        Row(
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        ) {
            EIconChip(icon = icon, tint = tint)
            Column(modifier = Modifier.weight(1f)) {
                Text(text = title, style = typo.cardTitle, color = colors.textPrimary)
                if (subtitle != null) {
                    Text(text = subtitle, style = typo.bodySmall, color = colors.textSecondary)
                }
            }
        }
        if (content != null) content()
    }
}
