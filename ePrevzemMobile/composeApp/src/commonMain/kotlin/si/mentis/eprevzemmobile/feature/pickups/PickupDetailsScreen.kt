package si.mentis.eprevzemmobile.feature.pickups

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.foundation.Canvas
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.ESummaryCard
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EBottomSheet
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EConfirmationDialog
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertBanner
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertType
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EPickupStatus
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EStatusChip
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.EPinPad
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EDivider
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.feature.pickups.model.PickupDetails
import si.mentis.eprevzemmobile.feature.pickups.model.UnlockPhase

@Composable
fun PickupDetailsScreen(
    state: PickupDetailsState,
    onEvent: (PickupDetailsEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    when (state.unlockPhase) {
        UnlockPhase.Idle -> IdlePhase(state = state, onEvent = onEvent, modifier = modifier)
        UnlockPhase.Unlocked -> UnlockedPhase(state = state, onEvent = onEvent, modifier = modifier)
        UnlockPhase.Confirmed -> {}
    }

    val removingDelegate = state.delegates.find { it.id == state.removingDelegateId }
    if (state.removingDelegateId != null && removingDelegate != null) {
        EConfirmationDialog(
            icon = EPrevzemIcons.profile(),
            title = "Odstrani pooblastilo?",
            message = "Ste prepričani, da želite odstraniti pooblastilo za ${removingDelegate.person.fullName}?",
            confirmLabel = "Odstrani",
            dismissLabel = "Prekliči",
            destructive = true,
            onConfirm = { onEvent(PickupDetailsEvent.RemoveDelegateConfirmed) },
            onDismiss = { onEvent(PickupDetailsEvent.RemoveDelegateCancelled) },
        )
    }

    if (state.showUnlockDialog) {
        EConfirmationDialog(
            icon = EPrevzemIcons.lock(),
            title = "Odkleni predalček?",
            message = "Z odklepanjem potrdite, da ste prisotni pri prevzemu in da boste vsebino predalčka dvignili osebno.",
            content = { LockerChip(state.details.lockerNumber) },
            confirmLabel = "Da, odkleni",
            dismissLabel = "Prekliči",
            onConfirm = { onEvent(PickupDetailsEvent.UnlockConfirmed) },
            onDismiss = { onEvent(PickupDetailsEvent.UnlockCancelled) },
        )
    }

    if (state.showBiometricSheet) {
        EBottomSheet(onDismiss = { onEvent(PickupDetailsEvent.UnlockCancelled) }) {
            val colors = EPrevzemTheme.colors
            val typo = EPrevzemTheme.typography
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(16.dp),
                modifier = Modifier.fillMaxWidth().padding(bottom = 8.dp),
            ) {
                Text(
                    text = "PREVERJANJE IDENTITETE",
                    style = typo.caption,
                    color = colors.textMuted,
                )
                BiometricSpinner()
                Text(
                    text = "Preverjamo identiteto …",
                    style = typo.section,
                    color = colors.textPrimary,
                )
                ETextButton(
                    label = "Uporabi PIN namesto biometrije",
                    onClick = { onEvent(PickupDetailsEvent.PinSelected) },
                )
                ESecondaryButton(
                    label = "Prekliči",
                    onClick = { onEvent(PickupDetailsEvent.UnlockCancelled) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        }
    }

    if (state.showPinSheet) {
        EBottomSheet(onDismiss = { onEvent(PickupDetailsEvent.UnlockCancelled) }) {
            val colors = EPrevzemTheme.colors
            val typo = EPrevzemTheme.typography
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(16.dp),
                modifier = Modifier.fillMaxWidth().padding(bottom = 8.dp),
            ) {
                Text(
                    text = "PREVERJANJE IDENTITETE",
                    style = typo.caption,
                    color = colors.textMuted,
                )
                Text(
                    text = "Vnesite PIN ePrevzem",
                    style = typo.section,
                    color = colors.textPrimary,
                )
                EPinPad(
                    value = state.pinValue,
                    onDigit = { digit -> onEvent(PickupDetailsEvent.PinDigitEntered(digit)) },
                    onBackspace = { onEvent(PickupDetailsEvent.PinBackspace) },
                    onSwitchToFallback = { onEvent(PickupDetailsEvent.BiometricSelected) },
                    switchFallbackLabel = "Uporabi biometrijo",
                )
                ESecondaryButton(
                    label = "Prekliči",
                    onClick = { onEvent(PickupDetailsEvent.UnlockCancelled) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        }
    }
}

@Composable
fun PickupDetailsRoute(
    pickupId: String,
    onBack: () -> Unit,
    onPickupConfirmed: (PickupDetails) -> Unit,
    onIdentityVerified: (PickupDetails) -> Unit,
    onLockerDidNotOpen: (PickupDetails) -> Unit = onIdentityVerified,
    onDelegatePerson: () -> Unit = {},
    initialUnlockedAt: String? = null,
    user: si.mentis.eprevzemmobile.domain.User? = null,
    repository: si.mentis.eprevzemmobile.data.pickups.PickupRepository = si.mentis.eprevzemmobile.AppContainer.pickupRepository,
    delegationRepository: si.mentis.eprevzemmobile.data.delegation.DelegationRepository = si.mentis.eprevzemmobile.AppContainer.delegationRepository,
    securityRepository: si.mentis.eprevzemmobile.data.security.SecurityRepository = si.mentis.eprevzemmobile.AppContainer.securityRepository,
    modifier: Modifier = Modifier,
) {
    val scope = rememberCoroutineScope()
    var state by remember(pickupId, initialUnlockedAt) {
        val starting = if (initialUnlockedAt != null) {
            PickupDetailsState(
                details = placeholderDetails(pickupId).copy(unlockedAt = initialUnlockedAt),
                unlockPhase = UnlockPhase.Unlocked,
                secondsRemaining = 30,
            )
        } else {
            PickupDetailsState(details = placeholderDetails(pickupId))
        }
        mutableStateOf(starting)
    }

    LaunchedEffect(pickupId) {
        val details = repository.getPickupDetails(pickupId)
        if (details != null) {
            state = state.copy(
                details = details.copy(unlockedAt = initialUnlockedAt ?: details.unlockedAt),
            )
        }
        state = state.copy(delegates = delegationRepository.getDelegations(pickupId))
    }

    fun verifyBiometric() {
        scope.launch {
            securityRepository.signChallengeWithBiometric("verify".encodeToByteArray())
                .onSuccess {
                    state = state.copy(showBiometricSheet = false)
                    onIdentityVerified(state.details)
                }
                .onFailure {
                    state = state.copy(showBiometricSheet = false, showPinSheet = true)
                }
        }
    }

    fun verifyPin(pin: String) {
        scope.launch {
            securityRepository.signChallengeWithPin(pin, "verify".encodeToByteArray())
                .onSuccess {
                    state = state.copy(showPinSheet = false, pinValue = "")
                    onIdentityVerified(state.details)
                }
                .onFailure {
                    // Could show error here if needed, but for now just clear pin
                    state = state.copy(pinValue = "")
                }
        }
    }

    LaunchedEffect(state.showBiometricSheet) {
        if (state.showBiometricSheet) {
            verifyBiometric()
        }
    }

    LaunchedEffect(state.unlockPhase) {
        if (state.unlockPhase == UnlockPhase.Unlocked) {
            while (state.secondsRemaining > 0) {
                delay(1000)
                state = state.copy(secondsRemaining = maxOf(0, state.secondsRemaining - 1))
            }
        }
    }

    LaunchedEffect(state.pendingRemoval) {
        val record = state.pendingRemoval ?: return@LaunchedEffect
        delegationRepository.removeDelegation(record.id)
            .onSuccess { state = state.copy(pendingRemoval = null) }
            .onFailure {
                state = state.copy(
                    pendingRemoval = null,
                    delegates = state.delegates + record,
                    delegateRemoveError = "Napaka pri odvzemu pooblastila. Poskusite znova.",
                )
            }
    }

    PickupDetailsScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                PickupDetailsEvent.Back -> onBack()
                PickupDetailsEvent.Share -> {}
                PickupDetailsEvent.UnlockClicked -> state = state.copy(showUnlockDialog = true)
                PickupDetailsEvent.UnlockConfirmed -> {
                    val biometricEnabled = user?.isBiometricEnabled ?: true
                    state = state.copy(
                        showUnlockDialog = false,
                        showBiometricSheet = biometricEnabled,
                        showPinSheet = !biometricEnabled,
                    )
                }
                PickupDetailsEvent.UnlockCancelled -> state = state.copy(
                    showUnlockDialog = false,
                    showBiometricSheet = false,
                    showPinSheet = false,
                )
                PickupDetailsEvent.BiometricSelected -> state = state.copy(
                    showPinSheet = false,
                    showBiometricSheet = true,
                )
                PickupDetailsEvent.PinSelected -> state = state.copy(
                    showBiometricSheet = false,
                    showPinSheet = true,
                )
                is PickupDetailsEvent.PinDigitEntered -> {
                    val newPin = if (state.pinValue.length < 6) {
                        state.pinValue + event.digit.toString()
                    } else {
                        state.pinValue
                    }
                    state = state.copy(pinValue = newPin)
                    if (newPin.length == 6) {
                        verifyPin(newPin)
                    }
                }
                PickupDetailsEvent.PinBackspace -> {
                    if (state.pinValue.isNotEmpty()) {
                        state = state.copy(pinValue = state.pinValue.dropLast(1))
                    }
                }
                PickupDetailsEvent.IdentityVerified -> onIdentityVerified(state.details)
                PickupDetailsEvent.Finish -> onPickupConfirmed(state.details)
                PickupDetailsEvent.LockerDidNotOpen -> onLockerDidNotOpen(state.details)
                PickupDetailsEvent.DelegatePersonClicked -> onDelegatePerson()
                is PickupDetailsEvent.RemoveDelegateClicked -> state = state.copy(
                    removingDelegateId = event.delegationId,
                    delegateRemoveError = null,
                )
                PickupDetailsEvent.RemoveDelegateCancelled -> state = state.copy(
                    removingDelegateId = null,
                )
                PickupDetailsEvent.RemoveDelegateConfirmed -> {
                    val record = state.delegates.find { it.id == state.removingDelegateId }
                    if (record != null) {
                        state = state.copy(
                            removingDelegateId = null,
                            delegates = state.delegates - record,
                            pendingRemoval = record,
                        )
                    }
                }
            }
        },
    )
}

@Composable
private fun IdlePhase(
    state: PickupDetailsState,
    onEvent: (PickupDetailsEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                eyebrow = "EPREVZEM",
                title = "Podrobnosti prevzema",
                onBack = { onEvent(PickupDetailsEvent.Back) },
                actionIcon = EPrevzemIcons.share(),
                onAction = { onEvent(PickupDetailsEvent.Share) },
            )
        },
    ) { _ ->
        EScreen {
            EStatusChip(status = state.details.status)

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(
                    text = state.details.title,
                    style = typo.display,
                    color = colors.textPrimary,
                )
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                ) {
                    Icon(
                        painter = EPrevzemIcons.organization(),
                        contentDescription = null,
                        tint = colors.textSecondary,
                        modifier = Modifier.size(14.dp),
                    )
                    Text(
                        text = state.details.organization,
                        style = typo.bodySmall,
                        color = colors.textSecondary,
                    )
                }
            }

            if (state.details.isExpiringSoon) {
                EAlertBanner(
                    type = EAlertType.Warning,
                    title = "Manj kot 24 ur do izteka roka.",
                    message = "Po izteku roka prevzem ni več možen. Dokument bo vrnjen pošiljatelju.",
                    icon = EPrevzemIcons.clock(),
                )
            }

            ESummaryCard(title = "Podrobnosti", icon = EPrevzemIcons.document()) {
                Column {
                    DetailRow(label = "Referenca", value = state.details.reference)
                    EDivider()
                    DetailRow(label = "Vrsta", value = state.details.type)
                    EDivider()
                    DetailRow(label = "Organizacija", value = state.details.organization)
                    EDivider()
                    DetailRow(label = "Na voljo od", value = state.details.availableFrom)
                    EDivider()
                    DetailRow(
                        label = "Prevzem do",
                        value = state.details.deadlineFormatted,
                        valueColor = if (state.details.isExpiringSoon) colors.warning else null,
                    )
                    EDivider()
                    DetailRowWithContent(label = "Status") { EStatusChip(status = state.details.status) }
                }
            }

            ESummaryCard(
                title = "Lokacija",
                icon = EPrevzemIcons.location(),
                trailing = { LockerChip(state.details.lockerNumber) },
            ) {
                MapPlaceholder(locationName = state.details.locationName)
                Text(
                    text = state.details.locationName,
                    style = typo.cardTitle,
                    color = colors.textPrimary,
                )
                Text(
                    text = state.details.locationAddress,
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                )
            }

            ESummaryCard(title = "Kako poteka prevzem", icon = EPrevzemIcons.info()) {
                NumberedStep(number = 1, text = "Kliknite »Odkleni predalček«.")
                NumberedStep(number = 2, text = "Potrdite identiteto z biometrijo ali PIN-om.")
                NumberedStep(number = 3, text = "Predalček se bo odprl za 30 sekund.")
                NumberedStep(number = 4, text = "Vzemite vsebino in zaprite vratca.")
            }

            ESummaryCard(title = "Pooblaščenci", icon = EPrevzemIcons.profile()) {
                if (state.delegateRemoveError != null) {
                    EAlertBanner(
                        type = EAlertType.Error,
                        title = "Napaka pri odvzemu pooblastila",
                        message = state.delegateRemoveError,
                    )
                }
                if (state.delegates.isEmpty()) {
                    Text(
                        text = "Nobena oseba ni pooblaščena za ta prevzem.",
                        style = typo.bodySmall,
                        color = colors.textSecondary,
                    )
                } else {
                    Column {
                        state.delegates.forEachIndexed { index, record ->
                            if (index > 0) EDivider()
                            DelegateRow(
                                record = record,
                                onRemove = { onEvent(PickupDetailsEvent.RemoveDelegateClicked(record.id)) },
                            )
                        }
                    }
                }
            }

            EPrimaryButton(
                label = "Odkleni predalček",
                icon = EPrevzemIcons.lock(),
                onClick = { onEvent(PickupDetailsEvent.UnlockClicked) },
                modifier = Modifier.fillMaxWidth(),
            )
            ESecondaryButton(
                label = "Pooblasti drugo osebo",
                icon = EPrevzemIcons.profile(),
                onClick = { onEvent(PickupDetailsEvent.DelegatePersonClicked) },
                modifier = Modifier.fillMaxWidth(),
            )
            ETextButton(
                label = "Imate težave?",
                onClick = {},
            )
        }
    }
}

@Composable
private fun UnlockedPhase(
    state: PickupDetailsState,
    onEvent: (PickupDetailsEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(modifier = modifier) { _ ->
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
            modifier = Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .navigationBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
        ) {
            UnlockIconBubble()
            Text(
                text = "Predalček je odklenjen",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "Prevzemite dokument iz predalčka.",
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
            UnlockedSummaryCard(details = state.details)
            Spacer(modifier = Modifier.weight(1f))
            EPrimaryButton(
                label = "Končaj",
                icon = EPrevzemIcons.check(),
                onClick = { onEvent(PickupDetailsEvent.Finish) },
                modifier = Modifier.fillMaxWidth(),
            )
            ETextButton(
                label = "Predalček se ni odprl",
                onClick = { onEvent(PickupDetailsEvent.LockerDidNotOpen) },
            )
        }
    }
}

@Composable
private fun DetailRow(
    label: String,
    value: String,
    valueColor: Color? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp),
    ) {
        Text(text = label, style = typo.bodySmall, color = colors.textSecondary)
        Text(text = value, style = typo.bodySmall, color = valueColor ?: colors.textPrimary)
    }
}

@Composable
private fun DetailRowWithContent(label: String, content: @Composable () -> Unit) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp),
    ) {
        Text(text = label, style = typo.bodySmall, color = colors.textSecondary)
        content()
    }
}

@Composable
private fun LockerChip(text: String) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        modifier = Modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(colors.primary50)
            .border(1.dp, colors.primary.copy(alpha = 0.2f), EPrevzemTheme.shapes.pill)
            .padding(horizontal = 12.dp, vertical = 6.dp),
    ) {
        Icon(
            painter = EPrevzemIcons.lock(),
            contentDescription = null,
            tint = colors.primary,
            modifier = Modifier.size(14.dp),
        )
        Text(
            text = text,
            style = typo.caption.copy(fontWeight = FontWeight.SemiBold),
            color = colors.primary,
        )
    }
}

@Composable
private fun MapPlaceholder(locationName: String) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val gridColor = colors.border

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(160.dp)
            .clip(EPrevzemTheme.shapes.medium)
            .background(colors.surfaceSunken),
    ) {
        Canvas(modifier = Modifier.fillMaxSize()) {
            val step = 32.dp.toPx()
            var x = 0f
            while (x <= size.width) {
                drawLine(gridColor, Offset(x, 0f), Offset(x, size.height), strokeWidth = 1f)
                x += step
            }
            var y = 0f
            while (y <= size.height) {
                drawLine(gridColor, Offset(0f, y), Offset(size.width, y), strokeWidth = 1f)
                y += step
            }
        }
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(44.dp)
                .align(Alignment.Center)
                .clip(CircleShape)
                .background(colors.primary),
        ) {
            Icon(
                painter = EPrevzemIcons.location(),
                contentDescription = null,
                tint = Color.White,
                modifier = Modifier.size(22.dp),
            )
        }
        Text(
            text = locationName,
            style = typo.caption.copy(fontWeight = FontWeight.Medium),
            color = colors.textPrimary,
            modifier = Modifier
                .align(Alignment.BottomStart)
                .padding(10.dp)
                .clip(EPrevzemTheme.shapes.pill)
                .background(Color.White)
                .padding(horizontal = 10.dp, vertical = 5.dp),
        )
    }
}

@Composable
private fun VerificationOptionRow(icon: androidx.compose.ui.graphics.painter.Painter, label: String) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(12.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(36.dp)
                .clip(CircleShape)
                .background(colors.primary50),
        ) {
            Icon(
                painter = icon,
                contentDescription = null,
                tint = colors.primary,
                modifier = Modifier.size(20.dp),
            )
        }
        Text(
            text = label,
            style = typo.bodySmall,
            color = colors.textPrimary,
            modifier = Modifier.weight(1f),
        )
    }
}

@Composable
private fun NumberedStep(number: Int, text: String) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        verticalAlignment = Alignment.Top,
        horizontalArrangement = Arrangement.spacedBy(12.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(24.dp)
                .clip(CircleShape)
                .background(colors.primary50),
        ) {
            Text(
                text = number.toString(),
                style = typo.caption.copy(fontWeight = FontWeight.Bold),
                color = colors.primary,
            )
        }
        Text(
            text = text,
            style = typo.bodySmall,
            color = colors.textSecondary,
            modifier = Modifier.weight(1f),
        )
    }
}

@Composable
private fun UnlockIconBubble() {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(80.dp)
            .clip(CircleShape)
            .background(colors.primary50),
    ) {
        Icon(
            painter = EPrevzemIcons.unlock(),
            contentDescription = null,
            tint = colors.primary,
            modifier = Modifier.size(40.dp),
        )
    }
}

@Composable
private fun CountdownChip(secondsRemaining: Int) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        modifier = Modifier
            .clip(EPrevzemTheme.shapes.pill)
            .background(colors.warningBg)
            .border(1.dp, colors.warning.copy(alpha = 0.2f), EPrevzemTheme.shapes.pill)
            .padding(horizontal = 16.dp, vertical = 8.dp),
    ) {
        Icon(
            painter = EPrevzemIcons.clock(),
            contentDescription = null,
            tint = colors.warning,
            modifier = Modifier.size(16.dp),
        )
        Text(
            text = "Predalček bo odprt še ${secondsRemaining} s",
            style = typo.caption.copy(fontWeight = FontWeight.SemiBold),
            color = colors.warning,
        )
    }
}

@Composable
private fun BiometricSpinner() {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier.size(80.dp),
    ) {
        CircularProgressIndicator(
            color = colors.primary,
            modifier = Modifier.size(80.dp),
            strokeWidth = 3.dp,
        )
        Icon(
            painter = EPrevzemIcons.biometric(),
            contentDescription = null,
            tint = colors.primary,
            modifier = Modifier.size(36.dp),
        )
    }
}

@Composable
private fun UnlockedSummaryCard(details: PickupDetails) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    ESummaryCard(icon = EPrevzemIcons.document()) {
        Text(text = details.title, style = typo.cardTitle, color = colors.textPrimary)
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            Icon(
                painter = EPrevzemIcons.organization(),
                contentDescription = null,
                tint = colors.textSecondary,
                modifier = Modifier.size(14.dp),
            )
            Text(text = details.organization, style = typo.bodySmall, color = colors.textSecondary)
        }
        EStatusChip(status = EPickupStatus.PickedUp)
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            Icon(
                painter = EPrevzemIcons.location(),
                contentDescription = null,
                tint = colors.textSecondary,
                modifier = Modifier.size(14.dp),
            )
            Text(text = details.locationName, style = typo.bodySmall, color = colors.textSecondary)
        }
        if (details.unlockedAt != null) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Icon(
                    painter = EPrevzemIcons.clock(),
                    contentDescription = null,
                    tint = colors.textSecondary,
                    modifier = Modifier.size(14.dp),
                )
                Text(
                    text = "Odklenjeno ob ${details.unlockedAt}",
                    style = typo.bodySmall,
                    color = colors.textSecondary,
                )
            }
        }
    }
}

@Composable
private fun DelegateRow(
    record: si.mentis.eprevzemmobile.data.delegation.DelegationRecord,
    onRemove: () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp),
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(text = record.person.fullName, style = typo.bodySmall, color = colors.textPrimary)
            Text(text = record.person.email, style = typo.bodySmall, color = colors.textSecondary)
        }
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(32.dp)
                .clickable(onClick = onRemove),
        ) {
            Icon(
                painter = EPrevzemIcons.close(),
                contentDescription = "Odstrani",
                tint = colors.error,
                modifier = Modifier.size(18.dp),
            )
        }
    }
}

private fun placeholderDetails(id: String) = PickupDetails(
    id = id, title = "", organization = "", reference = "", type = "",
    availableFrom = "", deadline = "", deadlineFormatted = "",
    status = EPickupStatus.Ready, isExpiringSoon = false,
    locationName = "", locationAddress = "", lockerNumber = "",
)
