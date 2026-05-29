package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsDivider
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsRow
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsSectionLabel
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun AuditLogScreen(entries: List<AuditLogEntry>){
    val colors=EPrevzemTheme.colors
    val typo=EPrevzemTheme.typography
    val spacing=EPrevzemTheme.spacing
    Column(verticalArrangement = Arrangement.spacedBy(spacing.cardGap)) {
        Column(verticalArrangement = Arrangement.spacedBy(spacing.xxs),
            modifier = Modifier.padding(horizontal = spacing.xxs)) {
            Text(
                text = "Zgodovina",
                style = typo.title,
                color = colors.textPrimary,
            )
        }

        EDetailsSectionLabel(
            title = "Dnevnik aktivnosti",
            hint = {
                Text(
                    text = "${entries.size} zapisa",
                    style = typo.bodySmall,
                    color = colors.textMuted,
                )
            },
        )

        entries.forEach { entry ->
            AuditLogCard(entry = entry)
        }
    }
}
@Composable
private fun AuditLogCard(entry: AuditLogEntry) {
    EDetailsCard {
        EDetailsRow(
            icon = EPrevzemIcons.document(),
            label = "Dokument",
            value = entry.documentTitle,
            tint = EIconTint.Green,
            trailing = { AuditStatusBadge(badge = entry.badge) },
        )
        EDetailsDivider()
        EDetailsRow(
            icon = EPrevzemIcons.organization(),
            label = "Organizacija",
            value = entry.organization,
            tint = EIconTint.Teal,
        )
        EDetailsDivider()
        EDetailsRow(
            icon = EPrevzemIcons.locker(),
            label = "Paketnik",
            value = entry.lockerNumber,
            tint = EIconTint.Gray,
        )
        EDetailsDivider()
        EDetailsRow(
            icon = EPrevzemIcons.clock(),
            label = "Odpiranje",
            value = entry.openedAt,
            tint = EIconTint.Gold,
        )
    }
}
@Composable
private fun AuditStatusBadge(badge: AuditLogBadge) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    val background = when (badge.tone) {
        AuditLogBadgeTone.Info -> colors.infoBg
        AuditLogBadgeTone.Success -> colors.successBg
        AuditLogBadgeTone.Warning -> colors.warningBg
        AuditLogBadgeTone.Error -> colors.errorBg
    }

    val foreground = when (badge.tone) {
        AuditLogBadgeTone.Info -> colors.info
        AuditLogBadgeTone.Success -> colors.success
        AuditLogBadgeTone.Warning -> colors.warning
        AuditLogBadgeTone.Error -> colors.error
    }

    Box(
        modifier = Modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(background)
            .padding(horizontal = spacing.sm, vertical = spacing.xxs),
    ) {
        Text(text = badge.label, style = typo.caption, color = foreground)
    }
}
