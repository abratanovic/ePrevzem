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
import androidx.compose.ui.unit.sp
import androidx.compose.runtime.LaunchedEffect
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.pickups.PickupRepository
import si.mentis.eprevzemmobile.domain.User
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EPickupCard
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EEmptyState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.ELoadingState
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

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
                    style = typo.display.copy(fontSize = 28.sp),
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
    user: User,
    onPickupClicked: (String) -> Unit,
    repository: PickupRepository = AppContainer.pickupRepository,
    modifier: Modifier = Modifier,
) {
    val scope = rememberCoroutineScope()
    var state by remember {
        mutableStateOf(ActivePickupsState(userName = user.fullName, activeTab = ActiveTab.Pickups))
    }

    LaunchedEffect(Unit) {
        state = state.copy(isRefreshing = true)
        state = state.copy(pickups = repository.getActivePickups(), isRefreshing = false)
    }

    ActivePickupsScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                ActivePickupsEvent.Refresh -> {
                    state = state.copy(isRefreshing = true)
                    scope.launch {
                        state = state.copy(pickups = repository.getActivePickups(), isRefreshing = false)
                    }
                }
                is ActivePickupsEvent.PickupClicked -> onPickupClicked(event.id)
                is ActivePickupsEvent.TabSelected -> state = state.copy(activeTab = event.tab)
            }
        },
    )
}
