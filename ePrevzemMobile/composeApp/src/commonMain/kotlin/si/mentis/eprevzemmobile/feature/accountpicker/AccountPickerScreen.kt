package si.mentis.eprevzemmobile.feature.accountpicker

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object AccountPickerStrings {
    const val Title = "Izberite račun"
    const val Subtitle = "Na tej napravi imate shranjenih več računov. Izberite, s katerim se želite prijaviti."
    const val Employee = "Zaposleni"
    const val Citizen = "Občan"
    const val AddAccount = "Dodaj račun"
}

@Composable
fun AccountPickerScreen(
    state: AccountPickerState,
    onEvent: (AccountPickerEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = AccountPickerStrings.Title,
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(
                        horizontal = EPrevzemTheme.spacing.screenHorizontal,
                        vertical = EPrevzemTheme.spacing.md,
                    ),
            ) {
                ESecondaryButton(
                    label = AccountPickerStrings.AddAccount,
                    icon = EPrevzemIcons.arrowRight(),
                    onClick = { onEvent(AccountPickerEvent.AddAccountClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        EScreen(verticalGap = EPrevzemTheme.spacing.md) {
            Text(
                text = AccountPickerStrings.Subtitle,
                style = EPrevzemTheme.typography.body,
                color = EPrevzemTheme.colors.textSecondary,
            )
            state.accounts.forEach { account ->
                AccountRowItem(
                    account = account,
                    onClick = { onEvent(AccountPickerEvent.AccountSelected(account.id)) },
                )
            }
        }
    }
}

@Composable
private fun AccountRowItem(
    account: AccountRow,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val shape = EPrevzemTheme.shapes.large
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.md),
        modifier = modifier
            .fillMaxWidth()
            .clip(shape)
            .background(colors.surface)
            .border(1.dp, colors.border, shape)
            .clickable(onClick = onClick)
            .padding(EPrevzemTheme.spacing.md),
    ) {
        EIconChip(
            painter = EPrevzemIcons.profile(),
            tint = if (account.type == AccountType.Employee) EIconTint.Teal else EIconTint.Green,
        )
        Column(
            verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.xs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = account.fullName,
                style = typo.body.copy(fontWeight = FontWeight.SemiBold),
                color = colors.textPrimary,
            )
            AccountTypeLabel(account = account)
        }
        Icon(
            painter = EPrevzemIcons.arrowRight(),
            contentDescription = null,
            tint = colors.textSecondary,
            modifier = Modifier.size(20.dp),
        )
    }
}

@Composable
private fun AccountTypeLabel(account: AccountRow) {
    val colors = EPrevzemTheme.colors
    val typeText = when (account.type) {
        AccountType.Employee -> AccountPickerStrings.Employee
        AccountType.Citizen -> AccountPickerStrings.Citizen
    }
    val label = account.organizationName
        ?.takeIf { it.isNotBlank() }
        ?.let { "$typeText · $it" }
        ?: typeText
    Row(
        modifier = Modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(colors.surfaceMuted)
            .padding(horizontal = 10.dp, vertical = 4.dp),
    ) {
        Text(
            text = label,
            style = EPrevzemTheme.typography.caption.copy(fontWeight = FontWeight.SemiBold),
            color = colors.textSecondary,
        )
    }
}
