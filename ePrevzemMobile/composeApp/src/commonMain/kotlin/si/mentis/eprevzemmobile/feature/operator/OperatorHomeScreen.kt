package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import androidx.compose.ui.text.font.FontWeight
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.cards.ESummaryCard
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.layout.ESectionHeader
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun OperatorHomeScreen(
    state: OperatorHomeState,
    onEvent: (OperatorHomeEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    val userInitials = state.userName
        .split(" ")
        .mapNotNull { it.firstOrNull()?.uppercaseChar() }
        .take(2)
        .joinToString("")

    val navItems = listOf(
        EBottomNavItem(
            id = OperatorTab.Pickups.name,
            icon = EPrevzemIcons.inbox(),
            label = "Prevzemi",
            primary = false,
        ),
        EBottomNavItem(
            id = OperatorTab.History.name,
            icon = EPrevzemIcons.history(),
            label = "Zgodovina",
            primary = false,
        ),
        EBottomNavItem(
            id = OperatorTab.Profile.name,
            icon = EPrevzemIcons.profile(),
            label = "Profil",
            primary = false,
        ),
    )

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Home,
                userInitials = userInitials,
            )
        },
        bottomBar = {
            EBottomNavigationBar(
                items = navItems,
                activeId = state.activeTab.name,
                onSelect = { tabId ->
                    OperatorTab.entries.find { it.name == tabId }?.let {
                        onEvent(OperatorHomeEvent.TabSelected(it))
                    }
                },
            )
        },
    ) {
        EScreen {
            Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
                Text(text = "DOBRODOŠLI", style = typo.caption, color = colors.textMuted)
                Text(
                    text = "Pozdravljeni, ${state.userName}",
                    style = typo.display,
                    color = colors.textPrimary,
                )
            }

            ScanPanel(
                onClick = { onEvent(OperatorHomeEvent.ScanQrClicked) },
                modifier = Modifier.fillMaxWidth(),
            )

            Column(verticalArrangement = Arrangement.spacedBy(spacing.sm)) {
                ESectionHeader(title = "Današnji pregled")
                OverviewCard(
                    title = "Za vstaviti",
                    value = state.pendingInsertionCount.toString(),
                    subtitle = "Dokumenti čakajo na vstavljanje",
                    icon = EPrevzemIcons.inbox(),
                    tint = EIconTint.Green,
                    modifier = Modifier.fillMaxWidth(),
                )
                OverviewCard(
                    title = "V paketniku",
                    value = state.inLockerCount.toString(),
                    subtitle = "Aktivni prevzemi v predalčkih",
                    icon = EPrevzemIcons.locker(),
                    tint = EIconTint.Teal,
                    modifier = Modifier.fillMaxWidth(),
                )
                OverviewCard(
                    title = "Zapadli",
                    value = state.expiredCount.toString(),
                    subtitle = "Prevzemi zahtevajo nadaljnjo obravnavo",
                    icon = EPrevzemIcons.warning(),
                    tint = EIconTint.Gold,
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        }
    }
}

@Composable
private fun ScanPanel(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    ESummaryCard(
        title = null,
        subtitle = null,
        tint = EIconTint.Green,
        modifier = modifier.clickable(onClick = onClick),
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = spacing.xs),
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(112.dp)
                    .clip(EPrevzemTheme.shapes.large)
                    .background(colors.primary50)
                    .border(1.dp, colors.primary100, EPrevzemTheme.shapes.large),
            ) {
                Icon(
                    painter = EPrevzemIcons.qrCode(),
                    contentDescription = null,
                    tint = colors.primary,
                    modifier = Modifier.size(52.dp),
                )
            }
            Text(
                text = "Skeniraj QR kodo na paketniku",
                style = typo.title,
                color = colors.textPrimary,
            )
            EPrimaryButton(
                label = "Začni skeniranje",
                onClick = onClick,
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(min = 52.dp),
            )
        }
    }
}

@Composable
private fun OverviewCard(
    title: String,
    value: String,
    subtitle: String,
    icon: androidx.compose.ui.graphics.painter.Painter,
    tint: EIconTint,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    ESummaryCard(
        title = title,
        subtitle = subtitle,
        icon = icon,
        tint = tint,
        modifier = modifier,
    ) {
        Text(
            text = value,
            style = typo.title.copy(fontWeight = FontWeight.Bold),
            color = colors.textPrimary,
            modifier = Modifier.padding(top = spacing.xxs),
        )
    }
}
