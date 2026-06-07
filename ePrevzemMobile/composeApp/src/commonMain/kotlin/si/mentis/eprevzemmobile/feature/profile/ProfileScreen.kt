package si.mentis.eprevzemmobile.feature.profile

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.Painter
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

@Immutable
data class ProfileUiState(
    val userName: String = "",
    val profile: ProfileData = ProfileData(),
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

    Column(verticalArrangement = Arrangement.spacedBy(spacing.xs)) {
        Text(
            text = "PROFIL",
            style = typo.caption,
            color = colors.textMuted,
        )
        Text(
            text = profile.fullName.ifBlank { state.userName },
            style = typo.title,
            color = colors.textPrimary,
        )
        Text(
            text = profile.organizationName,
            style = typo.body,
            color = colors.textSecondary,
        )
    }

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
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.inbox(),
                label = "E-pošta",
                value = profile.email,
                tint = EIconTint.Teal,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.notifications(),
                label = "Telefon",
                value = profile.phone,
                tint = EIconTint.Teal,
                mono = true,
            )
            EDetailsDivider()
            EDetailsRow(
                icon = EPrevzemIcons.clock(),
                label = "Veljavnost",
                value = "Do ${profile.validUntil}",
                tint = EIconTint.Gold,
            )
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
            EDetailsDivider()
            SettingsSwitchRow(
                icon = EPrevzemIcons.notifications(),
                title = "Obvestila o prevzemih",
                description = "Prejmite obvestilo, ko vas čaka nov dokument ali se bliža rok prevzema.",
                checked = state.areNotificationsEnabled,
                enabled = !state.isUpdatingSettings,
                tint = EIconTint.Teal,
                onCheckedChange = { enabled ->
                    onEvent(ProfileUiEvent.NotificationsToggled(enabled))
                },
            )
            EDetailsDivider()
            SettingsActionRow(
                icon = EPrevzemIcons.key(),
                title = "Spremeni PIN",
                description = "Zamenjajte 6-mestni PIN, ki ga uporabljate kot rezervno potrditev identitete.",
                enabled = !state.isUpdatingSettings && !state.isChangingPin,
                onClick = { onEvent(ProfileUiEvent.ChangePinClicked) },
            )
            EDetailsDivider()
            SettingsActionRow(
                icon = EPrevzemIcons.profile(),
                title = "Dodaj račun",
                description = "Registrirajte dodaten račun na tej napravi z registracijsko kodo.",
                enabled = true,
                onClick = { onEvent(ProfileUiEvent.AddAccountClicked) },
            )
        }
    }

    if (state.isBiometricPinSheetVisible) {
        BiometricPinSheet(state = state, onEvent = onEvent)
    }
    if (state.isChangePinSheetVisible) {
        ChangePinSheet(state = state, onEvent = onEvent)
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
