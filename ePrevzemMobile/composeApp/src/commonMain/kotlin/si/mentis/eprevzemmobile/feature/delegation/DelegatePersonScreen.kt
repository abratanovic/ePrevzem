package si.mentis.eprevzemmobile.feature.delegation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.cards.ESummaryCard
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorBanner
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ETextField
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object Strings {
    const val TopBarTitle = "Pooblasti drugo osebo"
    const val SearchHeading = "Iskanje po EMŠO"
    const val SearchDescription = "Vnesite EMŠO osebe, ki ji želite podeliti pooblastilo za prevzem."
    const val EmsoLabel = "EMŠO"
    const val EmsoPlaceholder = "0000000000000"
    const val SearchCta = "Išči"
    const val SearchingCta = "Iščem …"
    const val ErrorTitleFallback = "Napaka pri iskanju"
    const val PreviewHeading = "Najdena oseba"
    const val PreviewDescription = "Preverite, ali je to prava oseba, in potrdite pooblastilo."
    const val AuthorizeCta = "Pooblasti osebo"
    const val SearchAgainCta = "Išči drugega"
}

@Composable
fun DelegatePersonScreen(
    state: DelegatePersonState,
    onEvent: (DelegatePersonEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = Strings.TopBarTitle,
                onBack = { onEvent(DelegatePersonEvent.Back) },
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            when (state.phase) {
                DelegatePersonPhase.Preview -> PreviewActions(onEvent = onEvent)
                else -> SearchActions(state = state, onEvent = onEvent)
            }
        },
    ) { _ ->
        EScreen(verticalGap = EPrevzemTheme.spacing.xl) {
            when (state.phase) {
                DelegatePersonPhase.Preview -> PreviewPhaseContent(state = state)
                else -> SearchPhaseContent(state = state, onEvent = onEvent)
            }
        }
    }
}

@Composable
private fun SearchPhaseContent(
    state: DelegatePersonState,
    onEvent: (DelegatePersonEvent) -> Unit,
) {
    val typo = EPrevzemTheme.typography
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing

    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.xs),
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Text(
            text = Strings.SearchHeading,
            style = typo.title,
            color = colors.textPrimary,
            textAlign = TextAlign.Center,
        )
        Text(
            text = Strings.SearchDescription,
            style = typo.body,
            color = colors.textSecondary,
            textAlign = TextAlign.Center,
        )
    }

    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.xs),
        modifier = Modifier.fillMaxWidth(),
    ) {
        ETextField(
            value = state.emso,
            onValueChange = { raw ->
                onEvent(
                    DelegatePersonEvent.EmsoChanged(
                        raw.filter { it.isDigit() }.take(DelegatePersonState.EMSO_LENGTH)
                    )
                )
            },
            label = Strings.EmsoLabel,
            placeholder = Strings.EmsoPlaceholder,
            keyboardType = KeyboardType.Number,
            isError = state.hasError,
            enabled = !state.isLoading,
            modifier = Modifier.fillMaxWidth(),
        )
        if (state.hasError) {
            EErrorBanner(
                title = state.errorTitle ?: Strings.ErrorTitleFallback,
                message = state.errorMessage,
            )
        }
    }
}

@Composable
private fun PreviewPhaseContent(state: DelegatePersonState) {
    val person = state.foundPerson ?: return
    val typo = EPrevzemTheme.typography
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing

    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.xs),
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Text(
            text = Strings.PreviewHeading,
            style = typo.title,
            color = colors.textPrimary,
            textAlign = TextAlign.Center,
        )
        Text(
            text = Strings.PreviewDescription,
            style = typo.body,
            color = colors.textSecondary,
            textAlign = TextAlign.Center,
        )
    }

    ESummaryCard(
        title = person.fullName,
        subtitle = person.email,
        icon = EPrevzemIcons.profile(),
        tint = EIconTint.Teal,
    )
}

@Composable
private fun SearchActions(
    state: DelegatePersonState,
    onEvent: (DelegatePersonEvent) -> Unit,
) {
    val spacing = EPrevzemTheme.spacing
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .navigationBarsPadding()
            .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
    ) {
        EPrimaryButton(
            label = if (state.isLoading) Strings.SearchingCta else Strings.SearchCta,
            icon = if (state.isLoading) null else EPrevzemIcons.arrowRight(),
            enabled = state.canSearch,
            loading = state.isLoading,
            onClick = { onEvent(DelegatePersonEvent.SearchClicked) },
            modifier = Modifier.fillMaxWidth(),
        )
    }
}

@Composable
private fun PreviewActions(onEvent: (DelegatePersonEvent) -> Unit) {
    val spacing = EPrevzemTheme.spacing
    Column(
        verticalArrangement = Arrangement.spacedBy(spacing.sm),
        modifier = Modifier
            .fillMaxWidth()
            .navigationBarsPadding()
            .padding(horizontal = spacing.screenHorizontal, vertical = spacing.md),
    ) {
        EPrimaryButton(
            label = Strings.AuthorizeCta,
            icon = EPrevzemIcons.arrowRight(),
            onClick = { onEvent(DelegatePersonEvent.PersonSelected) },
            modifier = Modifier.fillMaxWidth(),
        )
        ESecondaryButton(
            label = Strings.SearchAgainCta,
            icon = EPrevzemIcons.back(),
            onClick = { onEvent(DelegatePersonEvent.Back) },
            modifier = Modifier.fillMaxWidth(),
        )
    }
}
