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
import androidx.compose.ui.graphics.painter.Painter
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
    // Icons must be resolved unconditionally (they are @Composable); the rows are
    // then assembled from whichever values are present so missing details are
    // dropped instead of rendered as placeholders.
    val documentIcon = EPrevzemIcons.document()
    val organizationIcon = EPrevzemIcons.organization()
    val lockerIcon = EPrevzemIcons.locker()
    val clockIcon = EPrevzemIcons.clock()

    val rows = buildList {
        entry.documentTitle?.let { add(AuditRowData(documentIcon, "Dokument", it, EIconTint.Green)) }
        entry.organization?.let { add(AuditRowData(organizationIcon, "Organizacija", it, EIconTint.Teal)) }
        entry.lockerNumber?.let { add(AuditRowData(lockerIcon, "Paketnik", it, EIconTint.Gray)) }
        add(AuditRowData(clockIcon, "Odpiranje", entry.openedAt, EIconTint.Gold))
    }

    EDetailsCard {
        rows.forEachIndexed { index, row ->
            if (index > 0) EDetailsDivider()
            EDetailsRow(
                icon = row.icon,
                label = row.label,
                value = row.value,
                tint = row.tint,
                // Keep the status badge visible by anchoring it to the first present row.
                trailing = if (index == 0) {
                    { AuditStatusBadge(badge = entry.badge) }
                } else {
                    null
                },
            )
        }
    }
}

private data class AuditRowData(
    val icon: Painter,
    val label: String,
    val value: String,
    val tint: EIconTint,
)
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
