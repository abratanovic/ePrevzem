package si.mentis.eprevzemmobile.feature.operator.insertion

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertBanner
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertType
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EEmptyState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.ELoadingState
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Stateless operator insertion screen. The camera for the station scan is
 * supplied as [scanner] by the route (which owns camera permission), keeping
 * this composable free of platform/permission state.
 */
@Composable
fun InsertionScreen(
    state: InsertionState,
    onEvent: (InsertionEvent) -> Unit,
    scanner: @Composable () -> Unit,
    modifier: Modifier = Modifier,
) {
    when (state.step) {
        InsertionStep.Scan -> ScanStep(onEvent = onEvent, scanner = scanner, error = state.error, modifier = modifier)
        InsertionStep.Loading -> Centered(modifier) { ELoadingState(message = "Nalagam paketnik …") }
        InsertionStep.Select -> SelectStep(state = state, onEvent = onEvent, modifier = modifier)
        InsertionStep.Opening -> Centered(modifier) { ELoadingState(message = "Odpiram predalček …") }
        InsertionStep.Opened -> OpenedStep(state = state, onEvent = onEvent, modifier = modifier)
        InsertionStep.Confirming -> Centered(modifier) { ELoadingState(message = "Shranjujem …") }
        InsertionStep.Done -> DoneStep(state = state, onEvent = onEvent, modifier = modifier)
    }
}

@Composable
private fun ScanStep(
    onEvent: (InsertionEvent) -> Unit,
    scanner: @Composable () -> Unit,
    error: String?,
    modifier: Modifier = Modifier,
) {
    val typo = EPrevzemTheme.typography
    Box(modifier = modifier.fillMaxSize().background(androidx.compose.ui.graphics.Color.Black)) {
        scanner()
        Column(modifier = Modifier.fillMaxSize().statusBarsPadding()) {
            ETopBar(
                variant = ETopBarVariant.Detail,
                eyebrow = "EPREVZEM",
                title = "Skeniraj paketnik",
                onBack = { onEvent(InsertionEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        }
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .align(Alignment.BottomCenter)
                .navigationBarsPadding()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            if (error != null) {
                EAlertBanner(type = EAlertType.Warning, title = error)
            }
            Text(
                text = "Skenirajte QR kodo na paketniku",
                style = typo.section,
                color = androidx.compose.ui.graphics.Color.White,
                textAlign = TextAlign.Center,
            )
        }
    }
}

@Composable
private fun SelectStep(
    state: InsertionState,
    onEvent: (InsertionEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing
    val context = state.context

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                eyebrow = context?.serialNumber ?: "PAKETNIK",
                title = "Vstavljanje paketa",
                onBack = { onEvent(InsertionEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
            ) {
                EPrimaryButton(
                    label = "Odpri predalček",
                    icon = EPrevzemIcons.lock(),
                    enabled = state.canOpen,
                    onClick = { onEvent(InsertionEvent.OpenClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        EScreen {
            if (state.error != null) {
                EAlertBanner(type = EAlertType.Error, title = state.error)
            }
            if (context == null || context.packages.isEmpty()) {
                EEmptyState(
                    icon = EPrevzemIcons.inbox(),
                    title = "Ni paketov za vstavljanje.",
                    message = "Na tem paketniku trenutno ni dokumentov, ki čakajo na vstavljanje.",
                )
                return@EScreen
            }

            Text(text = "Izberite paket", style = typo.section, color = colors.textPrimary)
            context.packages.forEach { pkg ->
                SelectableRow(
                    title = pkg.description,
                    subtitle = "${pkg.reference} · ${pkg.recipientName}",
                    selected = state.selectedPackageId == pkg.id,
                    onClick = { onEvent(InsertionEvent.PackageSelected(pkg.id)) },
                )
            }

            Text(text = "Izberite predalček", style = typo.section, color = colors.textPrimary)
            if (context.freeLockers.isEmpty()) {
                EAlertBanner(
                    type = EAlertType.Warning,
                    title = "Ni prostih predalčkov",
                    message = "Na tem paketniku trenutno ni prostih predalčkov.",
                )
            } else {
                context.freeLockers.forEach { locker ->
                    SelectableRow(
                        title = "Predalček ${locker.lockerNumber}",
                        subtitle = null,
                        selected = state.selectedLockerId == locker.lockerId,
                        onClick = { onEvent(InsertionEvent.LockerSelected(locker.lockerId)) },
                    )
                }
            }
        }
    }
}

@Composable
private fun OpenedStep(
    state: InsertionState,
    onEvent: (InsertionEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(
        modifier = modifier,
        bottomBar = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
                verticalArrangement = Arrangement.spacedBy(spacing.sm),
            ) {
                EPrimaryButton(
                    label = "Sem zaprl predalček",
                    icon = EPrevzemIcons.check(),
                    onClick = { onEvent(InsertionEvent.ConfirmClosedClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
                ESecondaryButton(
                    label = "Predalček se ni odprl",
                    onClick = { onEvent(InsertionEvent.OpenClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
        ) {
            IconBubble(EPrevzemIcons.unlock(), colors.primary, colors.primary50)
            Text(
                text = "Predalček ${state.selectedLocker?.lockerNumber ?: ""} je odprt",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "Vstavite paket »${state.selectedPackage?.description ?: ""}« in zaprite vratca.",
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
            if (state.error != null) {
                EAlertBanner(type = EAlertType.Error, title = state.error)
            }
        }
    }
}

@Composable
private fun DoneStep(
    state: InsertionState,
    onEvent: (InsertionEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(
        modifier = modifier,
        bottomBar = {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
            ) {
                EPrimaryButton(
                    label = "Končaj",
                    onClick = { onEvent(InsertionEvent.Done) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
        ) {
            IconBubble(EPrevzemIcons.check(), colors.success, colors.successBg)
            Text(
                text = "Paket vstavljen",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "Paket je shranjen v predalčku ${state.selectedLocker?.lockerNumber ?: ""}. Prejemnik je obveščen.",
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
        }
    }
}

@Composable
private fun SelectableRow(
    title: String,
    subtitle: String?,
    selected: Boolean,
    onClick: () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = Modifier
            .fillMaxWidth()
            .clip(EPrevzemTheme.shapes.medium)
            .background(if (selected) colors.primary50 else colors.surfaceMuted)
            .border(
                width = 1.dp,
                color = if (selected) colors.primary else colors.border,
                shape = EPrevzemTheme.shapes.medium,
            )
            .clickable(onClick = onClick)
            .padding(spacing.md),
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(text = title, style = typo.cardTitle, color = colors.textPrimary)
            if (subtitle != null) {
                Text(text = subtitle, style = typo.bodySmall, color = colors.textSecondary)
            }
        }
        if (selected) {
            Icon(
                painter = EPrevzemIcons.success(),
                contentDescription = null,
                tint = colors.primary,
                modifier = Modifier.size(22.dp),
            )
        } else {
            Box(
                modifier = Modifier
                    .size(22.dp)
                    .clip(CircleShape)
                    .border(1.dp, colors.border, CircleShape),
            )
        }
    }
}

@Composable
private fun IconBubble(
    icon: androidx.compose.ui.graphics.painter.Painter,
    tint: androidx.compose.ui.graphics.Color,
    bg: androidx.compose.ui.graphics.Color,
) {
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier.size(80.dp).clip(CircleShape).background(bg),
    ) {
        Icon(painter = icon, contentDescription = null, tint = tint, modifier = Modifier.size(40.dp))
    }
}

@Composable
private fun Centered(modifier: Modifier = Modifier, content: @Composable () -> Unit) {
    EScaffold(modifier = modifier) { _ ->
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { content() }
    }
}
