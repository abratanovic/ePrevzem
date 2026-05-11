package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.ESummaryCard
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EStatusChip
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails

@Composable
fun PickupConfirmedScreen(
    state: PickupConfirmedState,
    onEvent: (PickupConfirmedEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(modifier = modifier) { _ ->
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
        ) {
            SuccessIconBubble()
            Text(
                text = "Dokument je prevzet",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "Hvala. Prevzem je uspešno zaključen.",
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
            ConfirmedSummaryCard(details = state.details)
            Spacer(modifier = Modifier.weight(1f))
            EPrimaryButton(
                label = "Končaj",
                icon = EPrevzemIcons.check(),
                onClick = { onEvent(PickupConfirmedEvent.Finish) },
                modifier = Modifier.fillMaxWidth(),
            )
        }
    }
}

@Composable
fun PickupConfirmedRoute(
    details: PickupDetails,
    onFinish: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val state = remember(details) { PickupConfirmedState(details = details) }

    PickupConfirmedScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                PickupConfirmedEvent.Finish -> onFinish()
            }
        },
    )
}

@Composable
private fun SuccessIconBubble() {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(80.dp)
            .clip(CircleShape)
            .background(colors.primary50),
    ) {
        Icon(
            painter = EPrevzemIcons.success(),
            contentDescription = null,
            tint = colors.primary,
            modifier = Modifier.size(40.dp),
        )
    }
}

@Composable
private fun ConfirmedSummaryCard(details: PickupDetails) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    ESummaryCard(icon = EPrevzemIcons.document()) {
        Text(text = details.title, style = typo.cardTitle, color = colors.textPrimary)
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            Icon(
                painter = EPrevzemIcons.organization(),
                contentDescription = null,
                tint = colors.textSecondary,
                modifier = Modifier.size(14.dp),
            )
            Text(text = details.organization, style = typo.bodySmall, color = colors.textSecondary)
        }
        EStatusChip(status = EPickupStatus.PickedUp)
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            Icon(
                painter = EPrevzemIcons.location(),
                contentDescription = null,
                tint = colors.textSecondary,
                modifier = Modifier.size(14.dp),
            )
            Text(text = details.locationName, style = typo.bodySmall, color = colors.textSecondary)
        }
        if (details.unlockedAt != null) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Icon(
                    painter = EPrevzemIcons.clock(),
                    contentDescription = null,
                    tint = colors.textSecondary,
                    modifier = Modifier.size(14.dp),
                )
                Text(
                    text = "Prevzeto ob ${details.unlockedAt}",
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                )
            }
        }
    }
}
