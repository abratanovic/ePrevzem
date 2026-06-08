package si.mentis.eprevzemmobile.feature.documentprovision

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.AppContainer
import si.mentis.eprevzemmobile.core.camera.rememberPhotoCaptureLauncher
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorState
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.ELoadingState
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme
import si.mentis.eprevzemmobile.data.identity.DocumentVerificationException
import si.mentis.eprevzemmobile.data.identity.IdentityVerificationRepository

private object DocumentCaptureStrings {
    const val TopBarTitle = "Potrditev identitete"
    const val SelfieHeading = "Poslikajte obraz"
    const val SelfieDescription =
        "Naredite selfi v dobro osvetljenem prostoru. Obraz mora biti dobro viden."
    const val DocumentHeading = "Poslikajte dokument"
    const val DocumentDescription =
        "Poslikajte sprednji del dokumenta. Vsi podatki morajo biti čitljivi."
    const val VerifyingMessage = "Preverjanje identitete…"
    const val LaunchCamera = "Zaženite kamero"
    const val ErrorTitle = "Preverjanje ni uspelo"
    const val GenericError = "Napaka pri preverjanju. Preverite internetno povezavo."
}

private fun reasonToSlovenian(reason: String): String = when (reason) {
    "no_face_in_id" -> "Obraz na dokumentu ni bil zaznan."
    "no_face_in_selfie" -> "Obraz na selfiju ni bil zaznan."
    "liveness_failed" -> "Selfie ni bil prepoznan kot živa oseba."
    "face_mismatch" -> "Obraz se ne ujema z dokumentom."
    "document_ocr_failed" -> "Branje podatkov z dokumenta ni uspelo."
    "document_expired" -> "Dokument je potekel."
    "missing_name" -> "Ime ni bilo prebrano z dokumenta."
    "missing_surname" -> "Priimek ni bil prebran z dokumenta."
    "missing_emso" -> "EMŠO ni bil prebran z dokumenta."
    else -> reason
}

@Composable
fun DocumentCaptureScreen(
    state: DocumentCaptureState,
    onEvent: (DocumentCaptureEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    val spacing = EPrevzemTheme.spacing
    val colors = EPrevzemTheme.colors

    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = DocumentCaptureStrings.TopBarTitle,
                onBack = { onEvent(DocumentCaptureEvent.BackClicked) },
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            if (state.step == CaptureStep.SELFIE || state.step == CaptureStep.DOCUMENT) {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .navigationBarsPadding()
                        .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
                ) {
                    EPrimaryButton(
                        label = DocumentCaptureStrings.LaunchCamera,
                        icon = EPrevzemIcons.profile(),
                        onClick = { onEvent(DocumentCaptureEvent.CameraButtonClicked) },
                        modifier = Modifier.fillMaxWidth(),
                    )
                }
            }
        },
    ) { _ ->
        when (state.step) {
            CaptureStep.SELFIE -> CapturePrompt(
                heading = DocumentCaptureStrings.SelfieHeading,
                description = DocumentCaptureStrings.SelfieDescription,
                icon = EPrevzemIcons.profile(),
            )
            CaptureStep.DOCUMENT -> CapturePrompt(
                heading = DocumentCaptureStrings.DocumentHeading,
                description = DocumentCaptureStrings.DocumentDescription,
                icon = EPrevzemIcons.document(),
            )
            CaptureStep.VERIFYING -> Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier.fillMaxSize(),
            ) {
                ELoadingState(message = DocumentCaptureStrings.VerifyingMessage)
            }
            CaptureStep.ERROR -> {
                val message = if (state.errorReasons.isNotEmpty()) {
                    state.errorReasons.joinToString("\n") { reasonToSlovenian(it) }
                } else {
                    DocumentCaptureStrings.GenericError
                }
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier.fillMaxSize(),
                ) {
                    EErrorState(
                        title = DocumentCaptureStrings.ErrorTitle,
                        message = message,
                        onRetry = { onEvent(DocumentCaptureEvent.RetryClicked) },
                    )
                }
            }
        }
    }
}

@Composable
private fun CapturePrompt(
    heading: String,
    description: String,
    icon: Painter,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing

    EScreen(verticalGap = spacing.xl, modifier = modifier) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(104.dp)
                    .clip(CircleShape)
                    .background(colors.primary),
            ) {
                Icon(
                    painter = icon,
                    contentDescription = null,
                    tint = colors.textOnPrimary,
                    modifier = Modifier.size(48.dp),
                )
            }
        }

        Column(
            verticalArrangement = Arrangement.spacedBy(spacing.xs),
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text(
                text = heading,
                style = EPrevzemTheme.typography.title,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
            )
            Text(
                text = description,
                style = EPrevzemTheme.typography.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
            )
        }
    }
}

@Composable
fun DocumentCaptureRoute(
    variant: String,
    onCodeObtained: (code: String) -> Unit,
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
    repository: IdentityVerificationRepository = AppContainer.identityVerificationRepository,
) {
    var state by remember { mutableStateOf(DocumentCaptureState()) }

    val selfieCapture = rememberPhotoCaptureLauncher { bytes ->
        if (bytes != null) {
            state = state.copy(selfieBytes = bytes, step = CaptureStep.DOCUMENT)
        }
    }

    val documentCapture = rememberPhotoCaptureLauncher { bytes ->
        if (bytes != null) {
            state = state.copy(documentBytes = bytes, step = CaptureStep.VERIFYING)
        }
    }

    LaunchedEffect(state.step) {
        if (state.step != CaptureStep.VERIFYING) return@LaunchedEffect
        val selfie = state.selfieBytes ?: return@LaunchedEffect
        val doc = state.documentBytes ?: return@LaunchedEffect

        repository.verifyAndRegister(selfie, doc, variant)
            .onSuccess { code -> onCodeObtained(code) }
            .onFailure { ex ->
                val reasons = (ex as? DocumentVerificationException)?.reasons ?: emptyList()
                state = state.copy(step = CaptureStep.ERROR, errorReasons = reasons)
            }
    }

    DocumentCaptureScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                DocumentCaptureEvent.CameraButtonClicked -> when (state.step) {
                    CaptureStep.SELFIE -> selfieCapture.launch()
                    CaptureStep.DOCUMENT -> documentCapture.launch()
                    else -> {}
                }
                DocumentCaptureEvent.RetryClicked -> state = DocumentCaptureState()
                DocumentCaptureEvent.BackClicked -> onBack()
            }
        },
    )
}
