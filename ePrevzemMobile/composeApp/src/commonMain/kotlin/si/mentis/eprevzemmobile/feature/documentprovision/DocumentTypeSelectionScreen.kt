package si.mentis.eprevzemmobile.feature.documentprovision

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object DocumentTypeSelectionStrings {
    const val TopBarTitle = "Registracija z dokumentom"
    const val Heading = "Izberite vrsto dokumenta"
    const val Description =
        "Poslikajte se in svoj osebni dokument, da potrdite svojo identiteto."
    const val IdCard = "Osebna izkaznica"
    const val DrivingLicence = "Vozniško dovoljenje"
}

@Composable
fun DocumentTypeSelectionScreen(
    state: DocumentTypeSelectionState,
    onEvent: (DocumentTypeSelectionEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(
        modifier = modifier,
        topBar = {
            ETopBar(
                variant = ETopBarVariant.Detail,
                title = DocumentTypeSelectionStrings.TopBarTitle,
                onBack = { onEvent(DocumentTypeSelectionEvent.BackClicked) },
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            Column(
                verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.sm),
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(
                        horizontal = EPrevzemTheme.spacing.screenHorizontal,
                        vertical = EPrevzemTheme.spacing.md,
                    ),
            ) {
                EPrimaryButton(
                    label = DocumentTypeSelectionStrings.IdCard,
                    icon = EPrevzemIcons.document(),
                    onClick = { onEvent(DocumentTypeSelectionEvent.IdCardClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
                ESecondaryButton(
                    label = DocumentTypeSelectionStrings.DrivingLicence,
                    icon = EPrevzemIcons.document(),
                    onClick = { onEvent(DocumentTypeSelectionEvent.DrivingLicenceClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        EScreen(verticalGap = EPrevzemTheme.spacing.xl) {
            Column(
                verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.sm),
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    text = DocumentTypeSelectionStrings.Heading,
                    style = EPrevzemTheme.typography.title,
                    color = EPrevzemTheme.colors.textPrimary,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = DocumentTypeSelectionStrings.Description,
                    style = EPrevzemTheme.typography.body,
                    color = EPrevzemTheme.colors.textSecondary,
                    textAlign = TextAlign.Center,
                )
            }
        }
    }
}

@Composable
fun DocumentTypeSelectionRoute(
    onVariantSelected: (String) -> Unit,
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
) {
    DocumentTypeSelectionScreen(
        state = DocumentTypeSelectionState(),
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                DocumentTypeSelectionEvent.IdCardClicked -> onVariantSelected("id_card")
                DocumentTypeSelectionEvent.DrivingLicenceClicked -> onVariantSelected("driving_licence")
                DocumentTypeSelectionEvent.BackClicked -> onBack()
            }
        },
    )
}
