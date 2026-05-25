package si.mentis.eprevzemmobile.feature.registration.confirm

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
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.font.FontWeight
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsDivider
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsRow
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsSectionLabel
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EStatusChip
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESecurePinField
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESwitch
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIconSize
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
    const val SecuritySection = "Zaščita dostopa"
    const val SecurityHint = "Varnost"
    const val BiometricTitle = "Omogoči biometrično preverjanje"
    const val BiometricDescription =
        "Prstni odtis ali prepoznavo obraza bomo uporabili pred vsakim odpiranjem predalčka."
    const val PinTitle = "Nastavite PIN"
    const val PinDescription =
        "6-mestna številka. PIN je obvezen kot rezerva, tudi ko je vklopljena biometrija."
    const val PinLabel = "Nov PIN"
    const val PinConfirmationLabel = "Ponovite PIN"
    const val PinHelper = "Izberite kombinacijo, ki si jo boste zapomnili."
    const val PinConfirmationHelper = "Ponovite isto številko za potrditev."
    const val PinTooShortError = "PIN mora imeti 6 številk."
    const val PinMismatchError = "PIN-a se ne ujemata. Vnesite isto številko še enkrat."
    const val PinMatchSuccess = "PIN-a se ujemata."
    const val SubmitCta = "Potrdi in nadaljuj"
    const val UseAnotherCodeCta = "Vnesi drugo kodo"
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
        bottomBar = {
            ConfirmAccountActions(
                canSubmit = state.canSubmit,
                onSubmit = { onEvent(ConfirmAccountEvent.SubmitClicked) },
                onUseAnotherCode = { onEvent(ConfirmAccountEvent.UseAnotherCodeClicked) },
            )
        },
    ) { _ ->
        EScreen(verticalGap = spacing.lg) {
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
            SecuritySection(state = state, onEvent = onEvent)
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

@Composable
private fun SecuritySection(
    state: ConfirmAccountState,
    onEvent: (ConfirmAccountEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
        EDetailsSectionLabel(
            title = ConfirmAccountStrings.SecuritySection,
            hint = {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(spacing.xxs),
                ) {
                    Icon(
                        painter = EPrevzemIcons.lock(),
                        contentDescription = null,
                        tint = colors.textMuted,
                        modifier = Modifier.size(EPrevzemIconSize.Small),
                    )
                    Text(
                        text = ConfirmAccountStrings.SecurityHint,
                        style = typo.caption.copy(fontWeight = FontWeight.Medium),
                        color = colors.textMuted,
                    )
                }
            },
        )
        BiometricCard(
            checked = state.isBiometricEnabled,
            onCheckedChange = { enabled ->
                onEvent(ConfirmAccountEvent.BiometricToggled(enabled))
            },
        )
        PinSetupCard(state = state, onEvent = onEvent)
    }
}

@Composable
private fun BiometricCard(
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EDetailsCard {
        Row(
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        ) {
            EDetailsRowIcon(icon = EPrevzemIcons.biometric(), tint = EIconTint.Green)
            Column(
                verticalArrangement = Arrangement.spacedBy(spacing.xs),
                modifier = Modifier.weight(1f),
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(spacing.sm),
                ) {
                    Text(
                        text = ConfirmAccountStrings.BiometricTitle,
                        style = typo.cardTitle,
                        color = colors.textPrimary,
                        modifier = Modifier.weight(1f),
                    )
                    ESwitch(
                        checked = checked,
                        onCheckedChange = onCheckedChange,
                    )
                }
                Text(
                    text = ConfirmAccountStrings.BiometricDescription,
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                )
            }
        }
    }
}

@Composable
private fun PinSetupCard(
    state: ConfirmAccountState,
    onEvent: (ConfirmAccountEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EDetailsCard {
        Row(
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        ) {
            EDetailsRowIcon(icon = EPrevzemIcons.key(), tint = EIconTint.Teal)
            Column(
                verticalArrangement = Arrangement.spacedBy(spacing.xxs),
                modifier = Modifier.weight(1f),
            ) {
                Text(
                    text = ConfirmAccountStrings.PinTitle,
                    style = typo.cardTitle,
                    color = colors.textPrimary,
                )
                Text(
                    text = ConfirmAccountStrings.PinDescription,
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                )
            }
        }

        Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
            ESecurePinField(
                value = state.pin,
                onValueChange = { pin -> onEvent(ConfirmAccountEvent.PinChanged(pin)) },
                label = ConfirmAccountStrings.PinLabel,
                visible = state.isPinVisible,
                onVisibilityToggle = { onEvent(ConfirmAccountEvent.PinVisibilityToggled) },
                isError = state.isPinTooShort,
                modifier = Modifier.fillMaxWidth(),
            )
            FieldHelper(
                text = if (state.isPinTooShort) {
                    ConfirmAccountStrings.PinTooShortError
                } else {
                    ConfirmAccountStrings.PinHelper
                },
                tone = if (state.isPinTooShort) FieldHelperTone.Error else FieldHelperTone.Muted,
            )

            ESecurePinField(
                value = state.pinConfirmation,
                onValueChange = { pin ->
                    onEvent(ConfirmAccountEvent.PinConfirmationChanged(pin))
                },
                label = ConfirmAccountStrings.PinConfirmationLabel,
                visible = state.isPinConfirmationVisible,
                onVisibilityToggle = {
                    onEvent(ConfirmAccountEvent.PinConfirmationVisibilityToggled)
                },
                isError = state.isPinMismatch,
                modifier = Modifier.fillMaxWidth(),
            )
            FieldHelper(
                text = when {
                    state.isPinMismatch -> ConfirmAccountStrings.PinMismatchError
                    state.isPinValid -> ConfirmAccountStrings.PinMatchSuccess
                    else -> ConfirmAccountStrings.PinConfirmationHelper
                },
                tone = when {
                    state.isPinMismatch -> FieldHelperTone.Error
                    state.isPinValid -> FieldHelperTone.Success
                    else -> FieldHelperTone.Muted
                },
            )
        }
    }
}

@Composable
private fun ConfirmAccountActions(
    canSubmit: Boolean,
    onSubmit: () -> Unit,
    onUseAnotherCode: () -> Unit,
) {
    val spacing = EPrevzemTheme.spacing
    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = Modifier
            .fillMaxWidth()
            .navigationBarsPadding()
            .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
    ) {
        EPrimaryButton(
            label = ConfirmAccountStrings.SubmitCta,
            icon = EPrevzemIcons.arrowRight(),
            enabled = canSubmit,
            onClick = onSubmit,
            modifier = Modifier.fillMaxWidth(),
        )
        ESecondaryButton(
            label = ConfirmAccountStrings.UseAnotherCodeCta,
            icon = EPrevzemIcons.back(),
            onClick = onUseAnotherCode,
            modifier = Modifier.fillMaxWidth(),
        )
    }
}

@Composable
private fun EDetailsRowIcon(
    icon: Painter,
    tint: EIconTint,
) {
    EIconChip(
        painter = icon,
        tint = tint,
    )
}

private enum class FieldHelperTone { Muted, Error, Success }

@Composable
private fun FieldHelper(
    text: String,
    tone: FieldHelperTone,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val (icon, color) = when (tone) {
        FieldHelperTone.Muted -> EPrevzemIcons.info() to colors.textMuted
        FieldHelperTone.Error -> EPrevzemIcons.error() to colors.error
        FieldHelperTone.Success -> EPrevzemIcons.success() to colors.success
    }
    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(spacing.xs),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Icon(
            painter = icon,
            contentDescription = null,
            tint = color,
            modifier = Modifier.size(EPrevzemIconSize.Small),
        )
        Text(
            text = text,
            style = EPrevzemTheme.typography.caption,
            color = color,
            modifier = Modifier.weight(1f),
        )
    }
}
