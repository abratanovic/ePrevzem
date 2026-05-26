package si.mentis.eprevzemmobile.feature.login

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconChip
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorBanner
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.EPinPad
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object LoginStrings {
    const val BiometricTitle = "Dobrodošli nazaj"
    const val BiometricSubtitle = "Potrdite identiteto za dostop do ePrevzem."
    const val BiometricLoading = "Preverjanje biometrije..."
    const val SwitchToPinCta = "Vnesite PIN"
    const val PinTitle = "Vnesite PIN"
    const val PinSubtitle = "Za dostop do ePrevzem vnesite vaš 6-mestni PIN."
    const val SwitchToBiometricCta = "Biometrija"
    const val ResetSecureStorageCta = "Ponastavi varno shrambo"
}

@Composable
fun LoginScreen(
    state: LoginState,
    onEvent: (LoginEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(modifier = modifier) { _ ->
        when (state.phase) {
            LoginPhase.Biometric -> BiometricPhase(state = state, onEvent = onEvent)
            LoginPhase.Pin -> PinPhase(state = state, onEvent = onEvent)
        }
    }
}

@Composable
private fun BiometricPhase(
    state: LoginState,
    onEvent: (LoginEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = spacing.screenHorizontal),
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.md),
        ) {
            EIconChip(painter = EPrevzemIcons.biometric(), tint = EIconTint.Green)
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(spacing.xs),
            ) {
                Text(
                    text = LoginStrings.BiometricTitle,
                    style = typo.title,
                    color = colors.textPrimary,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = if (state.isLoading) LoginStrings.BiometricLoading else LoginStrings.BiometricSubtitle,
                    style = typo.body,
                    color = colors.textSecondary,
                    textAlign = TextAlign.Center,
                )
            }
            if (state.isLoading) {
                CircularProgressIndicator(
                    color = colors.primary,
                    modifier = Modifier.size(28.dp),
                    strokeWidth = 2.5.dp,
                )
            }
            if (state.error != null) {
                EErrorBanner(
                    title = state.error,
                    modifier = Modifier.fillMaxWidth(),
                )
            }
            ETextButton(
                label = LoginStrings.SwitchToPinCta,
                onClick = { onEvent(LoginEvent.SwitchToPinClicked) },
                enabled = !state.isLoading,
            )
            ETextButton(
                label = LoginStrings.ResetSecureStorageCta,
                onClick = { onEvent(LoginEvent.ResetSecureStorageClicked) },
                enabled = !state.isLoading,
            )
        }
    }
}

@Composable
private fun PinPhase(
    state: LoginState,
    onEvent: (LoginEvent) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val spacing = EPrevzemTheme.spacing

    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = spacing.screenHorizontal),
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(spacing.lg),
        ) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(spacing.xs),
            ) {
                Text(
                    text = LoginStrings.PinTitle,
                    style = typo.title,
                    color = colors.textPrimary,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = LoginStrings.PinSubtitle,
                    style = typo.body,
                    color = colors.textSecondary,
                    textAlign = TextAlign.Center,
                )
            }
            if (state.error != null) {
                EErrorBanner(
                    title = state.error,
                    modifier = Modifier.fillMaxWidth(),
                )
            }
            EPinPad(
                value = state.pin,
                onDigit = { digit -> onEvent(LoginEvent.PinDigitEntered(digit)) },
                onBackspace = { onEvent(LoginEvent.PinBackspaceClicked) },
                onSwitchToFallback = { onEvent(LoginEvent.BiometricRequested) },
                switchFallbackLabel = LoginStrings.SwitchToBiometricCta,
            )
            ETextButton(
                label = LoginStrings.ResetSecureStorageCta,
                onClick = { onEvent(LoginEvent.ResetSecureStorageClicked) },
                enabled = !state.isLoading,
            )
        }
    }
}
