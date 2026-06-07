package si.mentis.eprevzemmobile.feature.profile

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.avatar.EAvatar
import si.mentis.eprevzemmobile.core.designsystem.components.avatar.avatarInitials
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsDivider
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsRow
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsSectionLabel
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EBottomSheet
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorBanner
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESecurePinField
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESwitch
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Immutable
data class ProfileData(
    val fullName: String = "",
    val email: String = "",
    val phone: String = "",
    val status: String = "",
    val validUntil: String = "",
    val organizationName: String = "",
    val organizationType: String = "",
    val organizationLocation: String = "",
)

/** One identity registered on this device, as shown in the account switcher. */
@Immutable
data class ProfileAccount(
    val id: String,
    val fullName: String,
    val roleLabel: String,
    val isActive: Boolean,
)

@Immutable
data class ProfileUiState(
    val userName: String = "",
    val profile: ProfileData = ProfileData(),
    val accounts: List<ProfileAccount> = emptyList(),
    val isAccountSwitcherVisible: Boolean = false,
    val isBiometricEnabled: Boolean = false,
    val areNotificationsEnabled: Boolean = false,
    val isBiometricPinSheetVisible: Boolean = false,
    val biometricPin: String = "",
    val isBiometricPinVisible: Boolean = false,
    val isChangePinSheetVisible: Boolean = false,
    val currentPin: String = "",
    val newPin: String = "",
    val newPinConfirmation: String = "",
    val isCurrentPinVisible: Boolean = false,
    val isNewPinVisible: Boolean = false,
    val isNewPinConfirmationVisible: Boolean = false,
    val isChangingPin: Boolean = false,
    val pinChangeError: String? = null,
    val isUpdatingSettings: Boolean = false,
    val settingsError: String? = null,
) {
    val activeAccount: ProfileAccount? get() = accounts.firstOrNull { it.isActive }
    val canConfirmBiometric: Boolean get() = biometricPin.length == BIOMETRIC_PIN_LENGTH
    val isNewPinMismatch: Boolean get() =
        newPin.length == PIN_LENGTH && newPinConfirmation.length == PIN_LENGTH && newPin != newPinConfirmation
    val canConfirmPinChange: Boolean get() =
        currentPin.length == PIN_LENGTH &&
            newPin.length == PIN_LENGTH &&
            newPinConfirmation.length == PIN_LENGTH &&
            newPin == newPinConfirmation

    companion object {
        const val PIN_LENGTH = 6
        const val BIOMETRIC_PIN_LENGTH = PIN_LENGTH
    }
}

sealed interface ProfileUiEvent {
    data class BiometricToggleRequested(val enabled: Boolean) : ProfileUiEvent
    data class BiometricPinChanged(val pin: String) : ProfileUiEvent
    data object BiometricPinVisibilityToggled : ProfileUiEvent
    data object BiometricEnableConfirmed : ProfileUiEvent
    data object BiometricEnableCancelled : ProfileUiEvent
    data class NotificationsToggled(val enabled: Boolean) : ProfileUiEvent
    data object ChangePinClicked : ProfileUiEvent
    data object ChangePinCancelled : ProfileUiEvent
    data class CurrentPinChanged(val pin: String) : ProfileUiEvent
    data class NewPinChanged(val pin: String) : ProfileUiEvent
    data class NewPinConfirmationChanged(val pin: String) : ProfileUiEvent
    data object CurrentPinVisibilityToggled : ProfileUiEvent
    data object NewPinVisibilityToggled : ProfileUiEvent
    data object NewPinConfirmationVisibilityToggled : ProfileUiEvent
    data object ChangePinConfirmed : ProfileUiEvent
    data object AvatarClicked : ProfileUiEvent
    data object AccountSwitcherDismissed : ProfileUiEvent
    data class SwitchAccountRequested(val accountId: String) : ProfileUiEvent
    data object AddAccountClicked : ProfileUiEvent
}

/**
 * Stateless profile + security settings tab, shared by the citizen and operator flows.
 * Renders the profile summary, the settings card, and the biometric/PIN bottom sheets.
 */
@Composable
fun ProfileScreen(
    state: ProfileUiState,
    onEvent: (ProfileUiEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography
    val profile = state.profile

    ProfileHeaderControl(
        fullName = profile.fullName.ifBlank { state.userName },
        roleLabel = state.activeAccount?.roleLabel
            ?: profile.organizationName.takeIf { it.isNotBlank() }.orEmpty(),
        onClick = { onEvent(ProfileUiEvent.AvatarClicked) },
    )

    if (state.settingsError != null && !state.isBiometricPinSheetVisible) {
        EErrorBanner(title = state.settingsError)
    }

    Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
        EDetailsSectionLabel(title = "Uporabnik")
        EDetailsCard {
            EDetailsRow(
                icon = EPrevzemIcons.profile(),
                label = "Ime in priimek",
                value = profile.fullName,
            )
            if (profile.email.isNotEmpty()) {
                EDetailsDivider()
                EDetailsRow(
                    icon = EPrevzemIcons.inbox(),
                    label = "E-pošta",
                    value = profile.email,
                    tint = EIconTint.Teal,
                )
            }
            if (profile.phone.isNotEmpty()) {
                EDetailsDivider()
                EDetailsRow(
                    icon = EPrevzemIcons.notifications(),
                    label = "Telefon",
                    value = profile.phone,
                    tint = EIconTint.Teal,
                    mono = true,
                )
            }
            if (profile.validUntil.isNotEmpty()) {
                EDetailsDivider()
                EDetailsRow(
                    icon = EPrevzemIcons.clock(),
                    label = "Veljavnost",
                    value = "Do ${profile.validUntil}",
                    tint = EIconTint.Gold,
                )
            }
        }
    }

    Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
        EDetailsSectionLabel(title = "Nastavitve")
        EDetailsCard {
            SettingsSwitchRow(
                icon = EPrevzemIcons.biometric(),
                title = "Biometrično preverjanje",
                description = "Uporabite prstni odtis ali prepoznavo obraza pri odpiranju predalčka.",
                checked = state.isBiometricEnabled,
                enabled = !state.isUpdatingSettings,
                tint = EIconTint.Green,
                onCheckedChange = { enabled ->
                    onEvent(ProfileUiEvent.BiometricToggleRequested(enabled))
                },
            )
//            EDetailsDivider()
//            SettingsSwitchRow(
//                icon = EPrevzemIcons.notifications(),
//                title = "Obvestila o prevzemih",
//                description = "Prejmite obvestilo, ko vas čaka nov dokument ali se bliža rok prevzema.",
//                checked = state.areNotificationsEnabled,
//                enabled = !state.isUpdatingSettings,
//                tint = EIconTint.Teal,
//                onCheckedChange = { enabled ->
//                    onEvent(ProfileUiEvent.NotificationsToggled(enabled))
//                },
//            )
            EDetailsDivider()
            SettingsActionRow(
                icon = EPrevzemIcons.key(),
                title = "Spremeni PIN",
                description = "Zamenjajte 6-mestni PIN, ki ga uporabljate kot rezervno potrditev identitete.",
                enabled = !state.isUpdatingSettings && !state.isChangingPin,
                onClick = { onEvent(ProfileUiEvent.ChangePinClicked) },
            )
        }
    }

    if (state.isAccountSwitcherVisible) {
        AccountSwitcherSheet(state = state, onEvent = onEvent)
    }
    if (state.isBiometricPinSheetVisible) {
        BiometricPinSheet(state = state, onEvent = onEvent)
    }
    if (state.isChangePinSheetVisible) {
        ChangePinSheet(state = state, onEvent = onEvent)
    }
}

@Composable
private fun ProfileHeaderControl(
    fullName: String,
    roleLabel: String,
    onClick: () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography
    val shape = EPrevzemTheme.shapes.large

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(spacing.md),
        modifier = Modifier
            .fillMaxWidth()
            .clip(shape)
            .background(colors.surface)
            .border(1.dp, colors.border, shape)
            .clickable(onClick = onClick)
            .padding(spacing.md),
    ) {
        EAvatar(initials = avatarInitials(fullName), size = 56.dp)
        Column(
            verticalArrangement = Arrangement.spacedBy(spacing.xxs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = fullName,
                style = typo.title,
                color = colors.textPrimary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            if (roleLabel.isNotBlank()) {
                Text(
                    text = roleLabel,
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
        Icon(
            painter = EPrevzemIcons.unfoldMore(),
            contentDescription = "Zamenjaj račun",
            tint = colors.textMuted,
            modifier = Modifier.size(22.dp),
        )
    }
}

@Composable
private fun AccountSwitcherSheet(
    state: ProfileUiState,
    onEvent: (ProfileUiEvent) -> Unit,
) {
    val spacing = EPrevzemTheme.spacing

    EBottomSheet(
        title = "Zamenjaj račun",
        onDismiss = { onEvent(ProfileUiEvent.AccountSwitcherDismissed) },
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(spacing.xxs)) {
            state.accounts.forEach { account ->
                AccountSwitcherRow(
                    account = account,
                    onClick = {
                        if (!account.isActive) {
                            onEvent(ProfileUiEvent.SwitchAccountRequested(account.id))
                        }
                    },
                )
            }
            AddProfileRow(onClick = { onEvent(ProfileUiEvent.AddAccountClicked) })
        }
    }
}

@Composable
private fun AccountSwitcherRow(
    account: ProfileAccount,
    onClick: () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography
    val shape = EPrevzemTheme.shapes.medium

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(spacing.md),
        modifier = Modifier
            .fillMaxWidth()
            .clip(shape)
            .background(if (account.isActive) colors.primary50 else colors.surface)
            .clickable(enabled = !account.isActive, onClick = onClick)
            .padding(horizontal = spacing.sm, vertical = spacing.sm),
    ) {
        EAvatar(initials = avatarInitials(account.fullName), size = 44.dp)
        Column(
            verticalArrangement = Arrangement.spacedBy(spacing.xxs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = account.fullName,
                style = typo.body.copy(fontWeight = FontWeight.SemiBold),
                color = colors.textPrimary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            if (account.roleLabel.isNotBlank()) {
                Text(
                    text = account.roleLabel,
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
        if (account.isActive) {
            Icon(
                painter = EPrevzemIcons.success(),
                contentDescription = "Prijavljeni račun",
                tint = colors.primary,
                modifier = Modifier.size(22.dp),
            )
        }
    }
}

@Composable
private fun AddProfileRow(onClick: () -> Unit) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(spacing.md),
        modifier = Modifier
            .fillMaxWidth()
            .clip(EPrevzemTheme.shapes.medium)
            .clickable(onClick = onClick)
            .padding(horizontal = spacing.sm, vertical = spacing.sm),
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(44.dp)
                .clip(CircleShape)
                .background(colors.primary50)
                .border(1.dp, colors.primary100, CircleShape),
        ) {
            Icon(
                painter = EPrevzemIcons.add(),
                contentDescription = null,
                tint = colors.primary,
                modifier = Modifier.size(22.dp),
            )
        }
        Text(
            text = "Dodaj profil",
            style = typo.body.copy(fontWeight = FontWeight.SemiBold),
            color = colors.primary,
        )
    }
}

@Composable
private fun SettingsSwitchRow(
    icon: Painter,
    title: String,
    description: String,
    checked: Boolean,
    enabled: Boolean,
    tint: EIconTint,
    onCheckedChange: (Boolean) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = Modifier.fillMaxWidth(),
    ) {
        EIconChip(painter = icon, tint = tint)
        Column(
            verticalArrangement = Arrangement.spacedBy(spacing.xxs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = title,
                style = typo.cardTitle,
                color = colors.textPrimary,
            )
            Text(
                text = description,
                style = typo.bodySmall,
                color = colors.textSecondary,
            )
        }
        ESwitch(
            checked = checked,
            enabled = enabled,
            onCheckedChange = onCheckedChange,
        )
    }
}

@Composable
private fun SettingsActionRow(
    icon: Painter,
    title: String,
    description: String,
    enabled: Boolean,
    onClick: () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = Modifier.fillMaxWidth(),
    ) {
        EIconChip(painter = icon, tint = EIconTint.Gold)
        Column(
            verticalArrangement = Arrangement.spacedBy(spacing.xxs),
            modifier = Modifier.weight(1f),
        ) {
            Text(
                text = title,
                style = typo.cardTitle,
                color = colors.textPrimary,
            )
            Text(
                text = description,
                style = typo.bodySmall,
                color = colors.textSecondary,
            )
        }
        ESecondaryButton(
            label = "Uredi",
            onClick = onClick,
            enabled = enabled,
        )
    }
}

@Composable
private fun BiometricPinSheet(
    state: ProfileUiState,
    onEvent: (ProfileUiEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EBottomSheet(
        title = "Vklop biometrije",
        onDismiss = { onEvent(ProfileUiEvent.BiometricEnableCancelled) },
    ) {
        Text(
            text = "Za vklop biometričnega preverjanja najprej potrdite identiteto s PIN-om.",
            style = typo.body,
            color = colors.textSecondary,
        )

        if (state.settingsError != null) {
            EErrorBanner(title = state.settingsError)
        }

        ESecurePinField(
            value = state.biometricPin,
            onValueChange = { pin -> onEvent(ProfileUiEvent.BiometricPinChanged(pin)) },
            label = "PIN",
            visible = state.isBiometricPinVisible,
            onVisibilityToggle = { onEvent(ProfileUiEvent.BiometricPinVisibilityToggled) },
            enabled = !state.isUpdatingSettings,
            modifier = Modifier.fillMaxWidth(),
        )

        Row(
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
            modifier = Modifier.fillMaxWidth(),
        ) {
            ESecondaryButton(
                label = "Prekliči",
                onClick = { onEvent(ProfileUiEvent.BiometricEnableCancelled) },
                enabled = !state.isUpdatingSettings,
                modifier = Modifier.weight(1f),
            )
            EPrimaryButton(
                label = "Potrdi",
                onClick = { onEvent(ProfileUiEvent.BiometricEnableConfirmed) },
                enabled = state.canConfirmBiometric,
                loading = state.isUpdatingSettings,
                modifier = Modifier.weight(1f),
            )
        }
    }
}

@Composable
private fun ChangePinSheet(
    state: ProfileUiState,
    onEvent: (ProfileUiEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EBottomSheet(
        title = "Spremeni PIN",
        onDismiss = { onEvent(ProfileUiEvent.ChangePinCancelled) },
    ) {
        Text(
            text = "Vnesite trenutni PIN in izberite nov 6-mestni PIN.",
            style = typo.body,
            color = colors.textSecondary,
        )

        if (state.pinChangeError != null) {
            EErrorBanner(title = state.pinChangeError)
        }

        ESecurePinField(
            value = state.currentPin,
            onValueChange = { pin -> onEvent(ProfileUiEvent.CurrentPinChanged(pin)) },
            label = "Trenutni PIN",
            visible = state.isCurrentPinVisible,
            onVisibilityToggle = { onEvent(ProfileUiEvent.CurrentPinVisibilityToggled) },
            enabled = !state.isChangingPin,
            modifier = Modifier.fillMaxWidth(),
        )
        ESecurePinField(
            value = state.newPin,
            onValueChange = { pin -> onEvent(ProfileUiEvent.NewPinChanged(pin)) },
            label = "Nov PIN",
            visible = state.isNewPinVisible,
            onVisibilityToggle = { onEvent(ProfileUiEvent.NewPinVisibilityToggled) },
            enabled = !state.isChangingPin,
            modifier = Modifier.fillMaxWidth(),
        )
        ESecurePinField(
            value = state.newPinConfirmation,
            onValueChange = { pin -> onEvent(ProfileUiEvent.NewPinConfirmationChanged(pin)) },
            label = "Ponovite nov PIN",
            visible = state.isNewPinConfirmationVisible,
            onVisibilityToggle = { onEvent(ProfileUiEvent.NewPinConfirmationVisibilityToggled) },
            isError = state.isNewPinMismatch,
            enabled = !state.isChangingPin,
            modifier = Modifier.fillMaxWidth(),
        )

        if (state.isNewPinMismatch) {
            EErrorBanner(title = "Nova PIN-a se ne ujemata.")
        }

        Row(
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
            modifier = Modifier.fillMaxWidth(),
        ) {
            ESecondaryButton(
                label = "Prekliči",
                onClick = { onEvent(ProfileUiEvent.ChangePinCancelled) },
                enabled = !state.isChangingPin,
                modifier = Modifier.weight(1f),
            )
            EPrimaryButton(
                label = "Shrani",
                onClick = { onEvent(ProfileUiEvent.ChangePinConfirmed) },
                enabled = state.canConfirmPinChange,
                loading = state.isChangingPin,
                modifier = Modifier.weight(1f),
            )
        }
    }
}
