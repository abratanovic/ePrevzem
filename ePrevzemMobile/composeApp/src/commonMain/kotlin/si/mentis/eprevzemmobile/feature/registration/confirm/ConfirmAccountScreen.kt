package si.mentis.eprevzemmobile.feature.registration.confirm

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsDivider
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsRow
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsSectionLabel
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EStatusChip
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object ConfirmAccountStrings {
    const val TopBarTitle = "Potrditev računa"
    const val Heading = "Potrditev računa"
    const val Description = "Preverite, ali so podatki pravilni."
    const val AccountSection = "Račun"
    const val FullNameLabel = "Polno ime"
    const val EmailLabel = "E-pošta"
    const val PhoneLabel = "Telefon"
    const val ValidUntilLabel = "Veljavnost registracije"
    const val OrganizationSection = "Organizacija"
    const val OrganizationNameLabel = "Naziv"
    const val OrganizationTypeLabel = "Vrsta"
    const val OrganizationLocationLabel = "Lokacija"
}

@Composable
fun ConfirmAccountScreen(
    state: ConfirmAccountState,
    onEvent: (ConfirmAccountEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = ConfirmAccountStrings.TopBarTitle,
                onBack = { onEvent(ConfirmAccountEvent.BackClicked) },
                actionIcon = null,
                onAction = null,
            )
        },
    ) { _ ->
        EScreen(verticalGap = 18.dp) {
            Column(
                verticalArrangement = Arrangement.spacedBy(spacing.xs),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    text = ConfirmAccountStrings.Heading,
                    style = typo.title,
                    color = colors.textPrimary,
                )
                Text(
                    text = ConfirmAccountStrings.Description,
                    style = typo.body,
                    color = colors.textSecondary,
                )
            }

            AccountDetailsCard(account = state.account)
            OrganizationDetailsCard(organization = state.organization)
        }
    }
}

@Composable
private fun AccountDetailsCard(account: ConfirmAccountData) {
    Column(verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.xs)) {
        EDetailsSectionLabel(title = ConfirmAccountStrings.AccountSection)
        EDetailsCard {
            EDetailsRow(
                icon = EPrevzemIcons.profile(),
                label = ConfirmAccountStrings.FullNameLabel,
                value = account.fullName,
                trailing = {
                    EStatusChip(
                        status = EPickupStatus.Ready,
                        label = account.status,
                    )
                },
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.inbox(),
                label = ConfirmAccountStrings.EmailLabel,
                value = account.email,
                tint = EIconTint.Teal,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.notifications(),
                label = ConfirmAccountStrings.PhoneLabel,
                value = account.phone,
                tint = EIconTint.Teal,
                mono = true,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.clock(),
                label = ConfirmAccountStrings.ValidUntilLabel,
                value = "Do ${account.validUntil}",
                tint = EIconTint.Gold,
            )
        }
    }
}

@Composable
private fun OrganizationDetailsCard(organization: ConfirmOrganizationData) {
    Column(verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.xs)) {
        EDetailsSectionLabel(title = ConfirmAccountStrings.OrganizationSection)
        EDetailsCard {
            EDetailsRow(
                icon = EPrevzemIcons.organization(),
                label = ConfirmAccountStrings.OrganizationNameLabel,
                value = organization.name,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.shield(),
                label = ConfirmAccountStrings.OrganizationTypeLabel,
                value = organization.type,
                tint = EIconTint.Teal,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.location(),
                label = ConfirmAccountStrings.OrganizationLocationLabel,
                value = organization.location,
                tint = EIconTint.Teal,
            )
        }
    }
}
