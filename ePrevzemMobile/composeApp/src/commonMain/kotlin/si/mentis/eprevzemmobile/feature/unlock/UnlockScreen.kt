package si.mentis.eprevzemmobile.feature.unlock

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.camera.QrScannerView
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertBanner
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EAlertType
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun UnlockScreen(
    state: UnlockState,
    onEvent: (UnlockEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    when (val phase = state.phase) {
        UnlockPhase.RequestingPermission -> PermissionGate(onEvent = onEvent, modifier = modifier)
        UnlockPhase.Scanning -> ScanningPhase(onEvent = onEvent, modifier = modifier)
        is UnlockPhase.ScanError -> ScanErrorPhase(reason = phase.reason, onEvent = onEvent, modifier = modifier)
        UnlockPhase.Unlocking -> UnlockingPhase(modifier = modifier)
        is UnlockPhase.Failed -> FailedPhase(
            attempt = state.attempt,
            onEvent = onEvent,
            modifier = modifier,
        )
        UnlockPhase.Unlocked -> UnlockingPhase(modifier = modifier) // brief; route navigates away
    }
}

@Composable
private fun PermissionGate(
    onEvent: (UnlockEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                eyebrow = "EPREVZEM",
                title = "Skeniranje",
                onBack = { onEvent(UnlockEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        },
    ) { _ ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .navigationBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            contentAlignment = Alignment.Center,
        ) {
            CircularProgressIndicator(color = colors.primary)
        }
    }
}

@Composable
private fun ScanningPhase(
    onEvent: (UnlockEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    Box(modifier = modifier.fillMaxSize().background(Color.Black)) {
        QrScannerView(
            modifier = Modifier.fillMaxSize(),
            onQrDetected = { raw -> onEvent(UnlockEvent.QrDetected(raw)) },
        )
        ViewfinderOverlay()
        Column(
            modifier = Modifier.fillMaxSize().statusBarsPadding(),
        ) {
            ETopBar(
                variant = ETopBarVariant.Detail,
                eyebrow = "EPREVZEM",
                title = "Skeniraj predalček",
                onBack = { onEvent(UnlockEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        }
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .align(Alignment.BottomCenter)
                .navigationBarsPadding()
                .padding(horizontal = 24.dp, vertical = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text(
                text = "Skenirajte QR kodo na predalčku",
                style = typo.section,
                color = Color.White,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "QR koda mora ustrezati številki predalčka iz prevzema.",
                style = typo.bodySmall,
                color = Color.White.copy(alpha = 0.85f),
                textAlign = TextAlign.Center,
            )
        }
    }
}

@Composable
private fun ViewfinderOverlay() {
    val scrim = Color.Black.copy(alpha = 0.55f)
    val frameColor = Color.White
    Canvas(modifier = Modifier.fillMaxSize()) {
        val side = (minOf(size.width, size.height) * 0.72f)
        val left = (size.width - side) / 2f
        val top = (size.height - side) / 2f
        val right = left + side
        val bottom = top + side
        val cornerRadius = 24.dp.toPx()

        // Four scrim rectangles around the cutout
        drawRect(color = scrim, topLeft = Offset(0f, 0f), size = Size(size.width, top))
        drawRect(color = scrim, topLeft = Offset(0f, bottom), size = Size(size.width, size.height - bottom))
        drawRect(color = scrim, topLeft = Offset(0f, top), size = Size(left, side))
        drawRect(color = scrim, topLeft = Offset(right, top), size = Size(size.width - right, side))

        // Rounded frame around the cutout
        drawRoundRect(
            color = frameColor,
            topLeft = Offset(left, top),
            size = Size(side, side),
            cornerRadius = androidx.compose.ui.geometry.CornerRadius(cornerRadius, cornerRadius),
            style = androidx.compose.ui.graphics.drawscope.Stroke(width = 2.dp.toPx()),
        )
    }
}

@Composable
private fun ScanErrorPhase(
    reason: ScanErrorReason,
    onEvent: (UnlockEvent) -> Unit,
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
                eyebrow = "EPREVZEM",
                title = "Skeniranje",
                onBack = { onEvent(UnlockEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        },
    ) { _ ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .navigationBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            ErrorIconBubble(icon = when (reason) {
                ScanErrorReason.CameraDenied -> EPrevzemIcons.qrCode()
                else -> EPrevzemIcons.warning()
            })
            val (title, message) = when (reason) {
                ScanErrorReason.InvalidPayload ->
                    "Neveljavna QR koda" to "Skenirana koda ni veljavna številka predalčka."
                ScanErrorReason.WrongLocker ->
                    "Napačen predalček" to "Skenirana koda ne ustreza predalčku tega prevzema."
                ScanErrorReason.CameraDenied ->
                    "Dostop do kamere" to "Za skeniranje QR kode potrebujemo dostop do kamere."
            }
            Text(text = title, style = typo.display, color = colors.textPrimary, textAlign = TextAlign.Center)
            Text(
                text = message,
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
            EAlertBanner(
                type = EAlertType.Warning,
                title = "Poskusite znova",
                message = "Poravnajte QR kodo s predalčkom in počakajte, da se skenira.",
                icon = EPrevzemIcons.info(),
            )
            Box(modifier = Modifier.fillMaxWidth()) {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (reason == ScanErrorReason.CameraDenied) {
                        EPrimaryButton(
                            label = "Odpri nastavitve",
                            onClick = { onEvent(UnlockEvent.OpenSettings) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                        ESecondaryButton(
                            label = "Prekliči",
                            onClick = { onEvent(UnlockEvent.Back) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                    } else {
                        EPrimaryButton(
                            label = "Poskusite znova",
                            onClick = { onEvent(UnlockEvent.DismissScanError) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                        ETextButton(
                            label = "Prekliči",
                            onClick = { onEvent(UnlockEvent.Back) },
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun UnlockingPhase(modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    EScaffold(modifier = modifier) { _ ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .navigationBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier.size(96.dp),
            ) {
                CircularProgressIndicator(
                    color = colors.primary,
                    strokeWidth = 4.dp,
                    modifier = Modifier.size(96.dp),
                )
                Icon(
                    painter = EPrevzemIcons.unlock(),
                    contentDescription = null,
                    tint = colors.primary,
                    modifier = Modifier.size(42.dp),
                )
            }
            Box(modifier = Modifier.size(spacing.lg))
            Text(
                text = "Odklepanje…",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Box(modifier = Modifier.size(spacing.sm))
            Text(
                text = "Telefon predvaja zvočni signal. Približajte ga predalčku.",
                style = typo.bodySmall,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
        }
    }
}

@Composable
private fun FailedPhase(
    attempt: Int,
    onEvent: (UnlockEvent) -> Unit,
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
                eyebrow = "EPREVZEM",
                title = "Odklep ni uspel",
                onBack = { onEvent(UnlockEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        },
    ) { _ ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .navigationBarsPadding()
                .padding(horizontal = spacing.screenHorizontal, vertical = spacing.xl),
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            ErrorIconBubble(icon = EPrevzemIcons.warning())
            Text(
                text = "Odklep ni uspel",
                style = typo.display,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = "Poskusite znova ali kontaktirajte podporo.",
                style = typo.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
            EAlertBanner(
                type = EAlertType.Error,
                title = "Poskus $attempt od ${UnlockState.MAX_ATTEMPTS}",
                message = "Preverite, da je telefon blizu predalčka in zvok ni utišan.",
            )
            Box(modifier = Modifier.fillMaxWidth()) {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (attempt < UnlockState.MAX_ATTEMPTS) {
                        EPrimaryButton(
                            label = "Poskusite znova",
                            onClick = { onEvent(UnlockEvent.Retry) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                        ETextButton(
                            label = "Nazaj na podrobnosti",
                            onClick = { onEvent(UnlockEvent.Back) },
                        )
                    } else {
                        EPrimaryButton(
                            label = "Pokliči podporo",
                            onClick = { onEvent(UnlockEvent.ContactSupport) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                        ESecondaryButton(
                            label = "Nazaj na podrobnosti",
                            onClick = { onEvent(UnlockEvent.Back) },
                            modifier = Modifier.fillMaxWidth(),
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ErrorIconBubble(icon: androidx.compose.ui.graphics.painter.Painter) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(80.dp)
            .clip(CircleShape)
            .background(colors.errorBg)
            .border(1.dp, colors.error.copy(alpha = 0.2f), CircleShape),
    ) {
        Icon(
            painter = icon,
            contentDescription = null,
            tint = colors.error,
            modifier = Modifier.size(40.dp),
        )
    }
}
