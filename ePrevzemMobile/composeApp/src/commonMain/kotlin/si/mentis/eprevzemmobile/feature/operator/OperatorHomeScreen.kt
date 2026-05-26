package si.mentis.eprevzemmobile.feature.operator

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
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

    // Compute user initials from fullName
    val userInitials = state.userName
        .split(" ")
        .mapNotNull { it.firstOrNull()?.uppercaseChar() }
        .take(2)
        .joinToString("")

    // Bottom navigation items
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
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(text = "DOBRODOŠLI", style = typo.caption, color = colors.textMuted)
                Text(
                    text = "Pozdravljeni, ${state.userName}",
                    style = typo.display.copy(fontSize = 28.sp),
                    color = colors.textPrimary,
                )
            }
            Spacer(Modifier.height(24.dp))
            EPrimaryButton(
                label = "Skeniraj QR kodo na paketniku",
                onClick = { onEvent(OperatorHomeEvent.ScanQrClicked) },
                modifier = Modifier.fillMaxWidth(),
            )
        }
    }
}
