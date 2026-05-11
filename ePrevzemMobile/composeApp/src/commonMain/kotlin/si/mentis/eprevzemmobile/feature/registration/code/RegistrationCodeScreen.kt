package si.mentis.eprevzemmobile.feature.registration.code

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EConfirmationDialog
import si.mentis.eprevzemmobile.core.designsystem.components.feedback.EErrorBanner
import si.mentis.eprevzemmobile.core.designsystem.components.inputs.ETextField
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private object RegistrationCodeStrings {
    const val TopBarTitle = "Registracija naprave"
    const val Heading = "Vnesite registracijsko kodo"
    const val Description = "Vnesite registracijsko kodo, ki ste jo prejeli na spletni strani."
    const val FieldLabel = "Registracijska koda"
    const val FieldPlaceholder = "ABC-123-XYZ"
    const val HelpLink = "Kje najdem kodo?"
    const val Cta = "Preveri kodo"
    const val CtaLoading = "Preverjanje…"

    const val HelpTitle = "Kje najdem registracijsko kodo?"
    const val HelpMessage =
        "Registracijsko kodo prejmete na spletni strani po uspešni prijavi v vaš uporabniški račun." +
                "Koda poveže vašo napravo z vašim uporabniškim računom."
    const val HelpClose = "Razumem"
    const val ErrorTitleFallback = "Koda ni veljavna"
}

/**
 * Stateless device registration code screen. Renders [state] and emits
 * [RegistrationCodeEvent]s — never mutates state, performs IO, or decides
 * navigation. [RegistrationCodeRoute] wires up local state until a
 * ViewModel layer exists.
 */
@Composable
fun RegistrationCodeScreen(
    state: RegistrationCodeState,
    onEvent: (RegistrationCodeEvent) -> Unit,
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
                title = RegistrationCodeStrings.TopBarTitle,
                onBack = { onEvent(RegistrationCodeEvent.BackClicked) },
                actionIcon = null,
                onAction = null,
            )
        },
        bottomBar = {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(
                        horizontal = spacing.screenHorizontal,
                        vertical = spacing.md,
                    ),
            ) {
                EPrimaryButton(
                    label = if (state.isLoading) RegistrationCodeStrings.CtaLoading
                    else RegistrationCodeStrings.Cta,
                    icon = if (state.isLoading) null else EPrevzemIcons.arrowRight(),
                    enabled = state.canSubmit,
                    loading = state.isLoading,
                    onClick = { onEvent(RegistrationCodeEvent.SubmitClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
    ) { _ ->
        EScreen(verticalGap = spacing.xl) {
            HeroBadge(modifier = Modifier.fillMaxWidth())

            Column(
                verticalArrangement = Arrangement.spacedBy(spacing.xs),
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    text = RegistrationCodeStrings.Heading,
                    style = typo.title,
                    color = colors.textPrimary,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = RegistrationCodeStrings.Description,
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
                    value = state.code,
                    onValueChange = { raw ->
                        onEvent(RegistrationCodeEvent.CodeChanged(formatCode(raw)))
                    },
                    onClear = { onEvent(RegistrationCodeEvent.CodeChanged("")) },
                    label = RegistrationCodeStrings.FieldLabel,
                    placeholder = RegistrationCodeStrings.FieldPlaceholder,
                    isError = state.hasError,
                    enabled = !state.isLoading,
                    keyboardType = KeyboardType.Ascii,
                    textStyle = typo.mono.copy(
                        fontSize = 22.sp,
                        lineHeight = 26.sp,
                        letterSpacing = 1.5.sp,
                    ),
                    modifier = Modifier.fillMaxWidth(),
                )

                if (state.hasError) {
                    EErrorBanner(
                        title = state.errorTitle ?: RegistrationCodeStrings.ErrorTitleFallback,
                        message = state.errorMessage,
                    )
                }

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.End,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    ETextButton(
                        label = RegistrationCodeStrings.HelpLink,
                        icon = EPrevzemIcons.help(),
                        enabled = !state.isLoading,
                        onClick = { onEvent(RegistrationCodeEvent.HelpClicked) },
                    )
                }
            }

        }
    }

    if (state.isHelpVisible) {
        EConfirmationDialog(
            title = RegistrationCodeStrings.HelpTitle,
            message = RegistrationCodeStrings.HelpMessage,
            icon = EPrevzemIcons.help(),
            confirmLabel = RegistrationCodeStrings.HelpClose,
            onConfirm = { onEvent(RegistrationCodeEvent.HelpDismissed) },
            onDismiss = { onEvent(RegistrationCodeEvent.HelpDismissed) },
        )
    }
}

/**
 * Small concentric badge — primary50 halo + primary disc with the shield
 * icon. Visually echoes the welcome hero but at half the diameter so the
 * input remains the focal point.
 */
@Composable
private fun HeroBadge(icon: Painter = EPrevzemIcons.key(), modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    Box(modifier = modifier, contentAlignment = Alignment.Center) {
        Box(
            modifier = Modifier
                .size(96.dp)
                .clip(CircleShape)
                .background(colors.primary50),
        )
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(68.dp)
                .clip(CircleShape)
                .background(colors.primary),
        ) {
            Icon(
                painter = icon,
                contentDescription = null,
                tint = colors.textOnPrimary,
                modifier = Modifier.size(32.dp),
            )
        }
    }
}

/**
 * Strips non-alphanumerics, uppercases, caps at 9 characters, and rejoins
 * with hyphens in a 3-3-3 pattern. Pure function — safe to call from the
 * value-changed callback before emitting [RegistrationCodeEvent.CodeChanged].
 */
private fun formatCode(raw: String): String {
    val cleaned = raw
        .uppercase()
        .filter { it.isLetterOrDigit() }
        .take(RegistrationCodeState.CODE_LENGTH)
    return buildString {
        cleaned.forEachIndexed { index, c ->
            if (index == 3 || index == 6) append('-')
            append(c)
        }
    }
}
