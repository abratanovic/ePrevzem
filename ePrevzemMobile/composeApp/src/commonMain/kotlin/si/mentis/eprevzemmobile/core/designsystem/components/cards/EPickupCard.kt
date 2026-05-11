package si.mentis.eprevzemmobile.core.designsystem.components.cards

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EStatusChip
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun EPickupCard(
    title: String,
    organization: String,
    location: String,
    expires: String,
    status: EPickupStatus,
    modifier: Modifier = Modifier,
    onClick: (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    val base = cardModifier(modifier.fillMaxWidth())
    val rooted = if (onClick != null) base.clickable(onClick = onClick) else base

    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = rooted.padding(18.dp),
    ) {
        Row(
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        ) {
            EIconChip(icon = EPrevzemIcons.Document, tint = EIconTint.Green)
            Column(modifier = Modifier.weight(1f)) {
                Text(text = title, style = typo.cardTitle, color = colors.textPrimary)
                Text(text = organization, style = typo.bodySmall, color = colors.textSecondary)
            }
            EStatusChip(status = status)
        }
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(1.dp)
                .background(colors.divider),
        )
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween,
            modifier = Modifier.fillMaxWidth(),
        ) {
            IconText(EPrevzemIcons.Location, location)
            IconText(EPrevzemIcons.Clock, expires)
        }
    }
}

@Composable
private fun IconText(icon: androidx.compose.ui.graphics.vector.ImageVector, text: String) {
    val colors = EPrevzemTheme.colors
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = colors.textMuted,
            modifier = Modifier.size(14.dp),
        )
        Text(text = text, style = EPrevzemTheme.typography.caption, color = colors.textMuted)
    }
}
