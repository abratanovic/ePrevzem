package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EPickupCard
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EEmptyState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.ELoadingState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.feature.pickups.model.PickupItem

@Composable
fun ActivePickupsScreen(
    state: ActivePickupsState,
    onEvent: (ActivePickupsEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    val iconPickups = EPrevzemIcons.home()
    val iconHistory = EPrevzemIcons.history()
    val iconProfile = EPrevzemIcons.profile()
    val navItems = listOf(
        EBottomNavItem(id = ActiveTab.Pickups.name, icon = iconPickups, label = "Prevzemi"),
        EBottomNavItem(id = ActiveTab.History.name, icon = iconHistory, label = "Zgodovina"),
        EBottomNavItem(id = ActiveTab.Profile.name, icon = iconProfile, label = "Profil"),
    )

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Home,
                leadingIcon = EPrevzemIcons.organization(),
                userInitials = state.userName.split(" ")
                    .mapNotNull { it.firstOrNull()?.toString() }
                    .take(2)
                    .joinToString(""),
                actionIcon = null,
            )
        },
        bottomBar = {
            EBottomNavigationBar(
                items = navItems,
                activeId = state.activeTab.name,
                onSelect = { id ->
                    val tab = ActiveTab.entries.firstOrNull { it.name == id } ?: return@EBottomNavigationBar
                    onEvent(ActivePickupsEvent.TabSelected(tab))
                },
            )
        },
    ) { _ ->
        EScreen {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(
                    text = "DOBRODOŠLI NAZAJ",
                    style = typo.caption,
                    color = colors.textMuted,
                )
                Text(
                    text = "Pozdravljeni, ${state.userName}",
                    style = typo.display,
                    color = colors.textPrimary,
                )
            }

            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    text = "Aktivni prevzemi",
                    style = typo.section,
                    color = colors.textPrimary,
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = "${state.pickups.size} aktivni",
                    style = typo.bodySmall,
                    color = colors.textMuted,
                )
                Spacer(modifier = Modifier.weight(1f))
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(36.dp)
                        .clip(CircleShape)
                        .background(colors.surfaceMuted)
                        .clickable { onEvent(ActivePickupsEvent.Refresh) },
                ) {
                    Icon(
                        painter = EPrevzemIcons.refresh(),
                        contentDescription = "Osveži",
                        tint = colors.textSecondary,
                        modifier = Modifier.size(20.dp),
                    )
                }
            }

            if (state.isRefreshing) {
                ELoadingState(message = "Osvežujem seznam …")
            }

            if (!state.isRefreshing && state.pickups.isEmpty()) {
                EEmptyState(
                    icon = EPrevzemIcons.locker(),
                    title = "Trenutno nimate aktivnih prevzemov.",
                    message = "Ko bo organizacija pripravila dokument za prevzem, se bo pojavil tukaj.",
                )
            }

            state.pickups.forEach { pickup ->
                EPickupCard(
                    title = pickup.title,
                    organization = pickup.organization,
                    location = pickup.location,
                    expires = pickup.deadline,
                    lockerNumber = pickup.lockerNumber,
                    status = pickup.status,
                    warningText = if (pickup.isExpiringSoon) "Manj kot 24 ur do izteka roka prevzema." else null,
                    onClick = { onEvent(ActivePickupsEvent.PickupClicked(pickup.id)) },
                )
            }
        }
    }
}

@Composable
fun ActivePickupsRoute(
    onPickupClicked: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    val scope = rememberCoroutineScope()
    var state by remember {
        mutableStateOf(
            ActivePickupsState(
                userName = "Alenka Horvat",
                pickups = samplePickups(),
                activeTab = ActiveTab.Pickups,
            ),
        )
    }

    ActivePickupsScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                ActivePickupsEvent.Refresh -> {
                    state = state.copy(isRefreshing = true)
                    scope.launch {
                        delay(1500)
                        state = state.copy(isRefreshing = false, pickups = samplePickups())
                    }
                }
                is ActivePickupsEvent.PickupClicked -> onPickupClicked(event.id)
                is ActivePickupsEvent.TabSelected -> state = state.copy(activeTab = event.tab)
            }
        },
    )
}

private fun samplePickups(): List<PickupItem> = listOf(
    PickupItem(
        id = "1",
        title = "Osebna izkaznica",
        organization = "Upravna enota Ljubljana",
        location = "BTC City, Ljubljana",
        lockerNumber = "Paketnik #12",
        deadline = "15. 5. 2026",
        status = EPickupStatus.Ready,
        isExpiringSoon = false,
    ),
    PickupItem(
        id = "2",
        title = "Diploma",
        organization = "Univerza v Ljubljani",
        location = "Kongresni trg, Ljubljana",
        lockerNumber = "Paketnik #7",
        deadline = "12. 5. 2026",
        status = EPickupStatus.Expiring,
        isExpiringSoon = true,
    ),
    PickupItem(
        id = "3",
        title = "Potrdilo o stalnem bivališču",
        organization = "Mestna občina Ljubljana",
        location = "Magistrat, Ljubljana",
        lockerNumber = "Paketnik #3",
        deadline = "20. 5. 2026",
        status = EPickupStatus.Ready,
        isExpiringSoon = false,
    ),
)
