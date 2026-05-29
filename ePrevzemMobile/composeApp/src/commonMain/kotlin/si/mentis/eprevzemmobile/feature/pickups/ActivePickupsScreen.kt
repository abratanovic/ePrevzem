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
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.runtime.LaunchedEffect
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.data.logevent.LogEventRepository
import si.mentis.eprevzemmobile.data.pickups.PickupRepository
import si.mentis.eprevzemmobile.data.security.SecurityRepository
import si.mentis.eprevzemmobile.data.settings.UserSettingsRepository
import si.mentis.eprevzemmobile.domain.User
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EPickupCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EBottomSheet
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EEmptyState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorBanner
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.ELoadingState
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESecurePinField
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ESwitch
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavItem
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.EBottomNavigationBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import kotlin.enums.EnumEntries
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsDivider
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsRow
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EDetailsSectionLabel
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import androidx.compose.foundation.layout.padding
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
            when (state.activeTab){
                ActiveTab.Pickups->ActivePickupContent(
                    state=state,
                    onEvent=onEvent
                )
                ActiveTab.History -> AuditLogScreen(
                    entries=state.auditLogEntries
                )

                ActiveTab.Profile->ProfileSettingsContent(
                    state=state,
                    onEvent=onEvent
                )
            }
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
fun ActivePickupsRoute(
    user: User,
    onPickupClicked: (String) -> Unit,
    onUserUpdated: (User) -> Unit = {},
    repository: PickupRepository = AppContainer.pickupRepository,
    logEventRepository: LogEventRepository = AppContainer.logEventRepository,
    securityRepository: SecurityRepository = AppContainer.securityRepository,
    userSettingsRepository: UserSettingsRepository = AppContainer.userSettingsRepository,
    modifier: Modifier = Modifier,
) {
    val scope = rememberCoroutineScope()
    var state by remember {
        mutableStateOf(
            ActivePickupsState(
                userName = user.fullName,
                profile = user.toProfileData(),
                isBiometricEnabled = user.isBiometricEnabled,
                activeTab = ActiveTab.Pickups,
            )
        )
    }

    LaunchedEffect(user.id) {
        state = state.copy(isRefreshing = true)
        val (pickups, logEvents, biometricEnabled, notificationsEnabled) = coroutineScope {
            val pickups = async { repository.getActivePickups() }
            val logEvents = async { logEventRepository.getLogEventsForCurrentUser() }
            val biometricEnabled = async { securityRepository.isBiometricEnabled() }
            val notificationsEnabled = async { userSettingsRepository.areNotificationsEnabled() }
            LoadedActivePickupsData(
                pickups = pickups.await(),
                auditLogEntries = logEvents.await().map { it.toAuditLogEntry() },
                biometricEnabled = biometricEnabled.await(),
                notificationsEnabled = notificationsEnabled.await(),
            )
        }
        val syncedUser = user.copy(isBiometricEnabled = biometricEnabled)
        state = state.copy(
            pickups = pickups,
            auditLogEntries = logEvents,
            userName = user.fullName,
            profile = user.toProfileData(),
            isBiometricEnabled = biometricEnabled,
            areNotificationsEnabled = notificationsEnabled,
            isRefreshing = false,
        )
        if (syncedUser != user) onUserUpdated(syncedUser)
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
                is ActivePickupsEvent.BiometricToggleRequested -> {
                    if (event.enabled) {
                        state = state.copy(
                            isBiometricPinSheetVisible = true,
                            biometricPin = "",
                            isBiometricPinVisible = false,
                            settingsError = null,
                        )
                    } else if (!state.isUpdatingSettings) {
                        state = state.copy(isUpdatingSettings = true, settingsError = null)
                        scope.launch {
                            securityRepository.disableBiometric()
                                .onSuccess {
                                    val updatedUser = user.copy(isBiometricEnabled = false)
                                    state = state.copy(
                                        isBiometricEnabled = false,
                                        isUpdatingSettings = false,
                                        settingsError = null,
                                    )
                                    onUserUpdated(updatedUser)
                                }
                                .onFailure {
                                    state = state.copy(
                                        isUpdatingSettings = false,
                                        settingsError = "Biometrije ni bilo mogoče izklopiti. Poskusite znova.",
                                    )
                                }
                        }
                    }
                }
                is ActivePickupsEvent.BiometricPinChanged -> {
                    state = state.copy(
                        biometricPin = event.pin.take(ActivePickupsState.BIOMETRIC_PIN_LENGTH),
                        settingsError = null,
                    )
                }
                ActivePickupsEvent.BiometricPinVisibilityToggled -> {
                    state = state.copy(isBiometricPinVisible = !state.isBiometricPinVisible)
                }
                ActivePickupsEvent.BiometricEnableConfirmed -> {
                    if (state.canConfirmBiometric && !state.isUpdatingSettings) {
                        val pin = state.biometricPin
                        state = state.copy(isUpdatingSettings = true, settingsError = null)
                        scope.launch {
                            securityRepository.enableBiometric(pin)
                                .onSuccess {
                                    val updatedUser = user.copy(isBiometricEnabled = true)
                                    state = state.copy(
                                        isBiometricEnabled = true,
                                        isBiometricPinSheetVisible = false,
                                        biometricPin = "",
                                        isBiometricPinVisible = false,
                                        isUpdatingSettings = false,
                                        settingsError = null,
                                    )
                                    onUserUpdated(updatedUser)
                                }
                                .onFailure {
                                    state = state.copy(
                                        isUpdatingSettings = false,
                                        settingsError = "PIN ni pravilen ali biometrična potrditev ni uspela.",
                                    )
                                }
                        }
                    }
                }
                ActivePickupsEvent.BiometricEnableCancelled -> {
                    state = state.copy(
                        isBiometricPinSheetVisible = false,
                        biometricPin = "",
                        isBiometricPinVisible = false,
                        settingsError = null,
                    )
                }
                is ActivePickupsEvent.NotificationsToggled -> {
                    state = state.copy(
                        areNotificationsEnabled = event.enabled,
                        settingsError = null,
                    )
                    scope.launch {
                        userSettingsRepository.setNotificationsEnabled(event.enabled)
                    }
                }
                ActivePickupsEvent.ChangePinClicked -> {
                    state = state.copy(
                        isChangePinSheetVisible = true,
                        currentPin = "",
                        newPin = "",
                        newPinConfirmation = "",
                        isCurrentPinVisible = false,
                        isNewPinVisible = false,
                        isNewPinConfirmationVisible = false,
                        pinChangeError = null,
                    )
                }
                ActivePickupsEvent.ChangePinCancelled -> {
                    state = state.copy(
                        isChangePinSheetVisible = false,
                        currentPin = "",
                        newPin = "",
                        newPinConfirmation = "",
                        isCurrentPinVisible = false,
                        isNewPinVisible = false,
                        isNewPinConfirmationVisible = false,
                        isChangingPin = false,
                        pinChangeError = null,
                    )
                }
                is ActivePickupsEvent.CurrentPinChanged -> {
                    state = state.copy(
                        currentPin = event.pin.take(ActivePickupsState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                is ActivePickupsEvent.NewPinChanged -> {
                    state = state.copy(
                        newPin = event.pin.take(ActivePickupsState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                is ActivePickupsEvent.NewPinConfirmationChanged -> {
                    state = state.copy(
                        newPinConfirmation = event.pin.take(ActivePickupsState.PIN_LENGTH),
                        pinChangeError = null,
                    )
                }
                ActivePickupsEvent.CurrentPinVisibilityToggled -> {
                    state = state.copy(isCurrentPinVisible = !state.isCurrentPinVisible)
                }
                ActivePickupsEvent.NewPinVisibilityToggled -> {
                    state = state.copy(isNewPinVisible = !state.isNewPinVisible)
                }
                ActivePickupsEvent.NewPinConfirmationVisibilityToggled -> {
                    state = state.copy(isNewPinConfirmationVisible = !state.isNewPinConfirmationVisible)
                }
                ActivePickupsEvent.ChangePinConfirmed -> {
                    if (state.canConfirmPinChange && !state.isChangingPin) {
                        val currentPin = state.currentPin
                        val newPin = state.newPin
                        state = state.copy(isChangingPin = true, pinChangeError = null)
                        scope.launch {
                            securityRepository.changePin(currentPin, newPin)
                                .onSuccess {
                                    state = state.copy(
                                        isChangePinSheetVisible = false,
                                        currentPin = "",
                                        newPin = "",
                                        newPinConfirmation = "",
                                        isCurrentPinVisible = false,
                                        isNewPinVisible = false,
                                        isNewPinConfirmationVisible = false,
                                        isChangingPin = false,
                                        pinChangeError = null,
                                    )
                                }
                                .onFailure {
                                    state = state.copy(
                                        isChangingPin = false,
                                        pinChangeError = "Trenutni PIN ni pravilen. Poskusite znova.",
                                    )
                                }
                        }
                    }
                }
            }
        },
    )
}

private data class LoadedActivePickupsData(
    val pickups: List<si.mentis.eprevzemmobile.feature.pickups.model.PickupItem>,
    val auditLogEntries: List<AuditLogEntry>,
    val biometricEnabled: Boolean,
    val notificationsEnabled: Boolean,
)

private fun User.toProfileData(): ProfileData = ProfileData(
    fullName = fullName,
    email = email,
    phone = phone,
    status = status,
    validUntil = validUntil,
    organizationName = organizationName,
    organizationType = organizationType,
    organizationLocation = organizationLocation,
)

@Composable
private fun ProfileSettingsContent(
    state: ActivePickupsState,
    onEvent: (ActivePickupsEvent) -> Unit,
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
                    onEvent(ActivePickupsEvent.BiometricToggleRequested(enabled))
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
                    onEvent(ActivePickupsEvent.NotificationsToggled(enabled))
                },
            )
            EDetailsDivider()
            SettingsActionRow(
                icon = EPrevzemIcons.key(),
                title = "Spremeni PIN",
                description = "Zamenjajte 6-mestni PIN, ki ga uporabljate kot rezervno potrditev identitete.",
                enabled = !state.isUpdatingSettings && !state.isChangingPin,
                onClick = { onEvent(ActivePickupsEvent.ChangePinClicked) },
            )
        }
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
    state: ActivePickupsState,
    onEvent: (ActivePickupsEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EBottomSheet(
        title = "Vklop biometrije",
        onDismiss = { onEvent(ActivePickupsEvent.BiometricEnableCancelled) },
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
            onValueChange = { pin -> onEvent(ActivePickupsEvent.BiometricPinChanged(pin)) },
            label = "PIN",
            visible = state.isBiometricPinVisible,
            onVisibilityToggle = { onEvent(ActivePickupsEvent.BiometricPinVisibilityToggled) },
            enabled = !state.isUpdatingSettings,
            modifier = Modifier.fillMaxWidth(),
        )

        Row(
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
            modifier = Modifier.fillMaxWidth(),
        ) {
            ESecondaryButton(
                label = "Prekliči",
                onClick = { onEvent(ActivePickupsEvent.BiometricEnableCancelled) },
                enabled = !state.isUpdatingSettings,
                modifier = Modifier.weight(1f),
            )
            EPrimaryButton(
                label = "Potrdi",
                onClick = { onEvent(ActivePickupsEvent.BiometricEnableConfirmed) },
                enabled = state.canConfirmBiometric,
                loading = state.isUpdatingSettings,
                modifier = Modifier.weight(1f),
            )
        }
    }
}

@Composable
private fun ChangePinSheet(
    state: ActivePickupsState,
    onEvent: (ActivePickupsEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val typo = EPrevzemTheme.typography

    EBottomSheet(
        title = "Spremeni PIN",
        onDismiss = { onEvent(ActivePickupsEvent.ChangePinCancelled) },
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
            onValueChange = { pin -> onEvent(ActivePickupsEvent.CurrentPinChanged(pin)) },
            label = "Trenutni PIN",
            visible = state.isCurrentPinVisible,
            onVisibilityToggle = { onEvent(ActivePickupsEvent.CurrentPinVisibilityToggled) },
            enabled = !state.isChangingPin,
            modifier = Modifier.fillMaxWidth(),
        )
        ESecurePinField(
            value = state.newPin,
            onValueChange = { pin -> onEvent(ActivePickupsEvent.NewPinChanged(pin)) },
            label = "Nov PIN",
            visible = state.isNewPinVisible,
            onVisibilityToggle = { onEvent(ActivePickupsEvent.NewPinVisibilityToggled) },
            enabled = !state.isChangingPin,
            modifier = Modifier.fillMaxWidth(),
        )
        ESecurePinField(
            value = state.newPinConfirmation,
            onValueChange = { pin -> onEvent(ActivePickupsEvent.NewPinConfirmationChanged(pin)) },
            label = "Ponovite nov PIN",
            visible = state.isNewPinConfirmationVisible,
            onVisibilityToggle = { onEvent(ActivePickupsEvent.NewPinConfirmationVisibilityToggled) },
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
                onClick = { onEvent(ActivePickupsEvent.ChangePinCancelled) },
                enabled = !state.isChangingPin,
                modifier = Modifier.weight(1f),
            )
            EPrimaryButton(
                label = "Shrani",
                onClick = { onEvent(ActivePickupsEvent.ChangePinConfirmed) },
                enabled = state.canConfirmPinChange,
                loading = state.isChangingPin,
                modifier = Modifier.weight(1f),
            )
        }
    }
}

@Composable
private fun ActivePickupContent(
    state: ActivePickupsState,
    onEvent: (ActivePickupsEvent) -> Unit
){
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
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
