package si.mentis.eprevzemmobile.feature.onboarding

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.components.cards.ESummaryCard
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EIconTint
import si.mentis.eprevzemmobile.core.designsystem.components.cards.EInfoCard
import si.mentis.eprevzemmobile.core.designsystem.components.dialogs.EConfirmationDialog
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScaffold
import si.mentis.eprevzemmobile.core.designsystem.components.layout.EScreen
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBar
import si.mentis.eprevzemmobile.core.designsystem.components.navigation.ETopBarVariant
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

// Slovenian copy is inline for now. Move to a string resource bundle when
// localisation lands; the screen is otherwise translation-ready.
private object WelcomeStrings {
    const val Title = "Dobrodošli v ePrevzem"
    const val Description =
        "Aplikacija je namenjena varnemu prevzemu dokumentov iz pametnih " +
            "paketnikov. Identiteto potrdite na telefonu, paketnik pa se odklene samodejno."
    const val InfoTitle = "Registracijska koda"
    const val InfoBody =
        "Za uporabo aplikacije potrebujete registracijsko kodo, ki ste jo " +
            "prejeli ob registraciji na ePrevzem spletni strani."
    const val RegisterCta = "Registriraj napravo"
    const val HelpLink = "Kaj je registracijska koda?"
    const val HelpTitle = "Kaj je registracijska koda?"
    const val HelpMessage =
        "Registracijsko kodo prejmete na spletni strani po uspešni prijavi v vaš uporabniški račun." +
            "Koda poveže vašo napravo z vašim uporabniškim računom."
    const val HelpClose = "Razumem"
}

/**
 * Stateless welcome screen. Hosts no business logic and never decides what
 * happens on `Registriraj napravo` — that's the caller's job through
 * [onRegisterDeviceClick] (or the [WelcomeEvent.RegisterDeviceClicked]
 * event, depending on which entry point the caller wires up).
 */
@Composable
fun WelcomeScreen(
    state: WelcomeState,
    onEvent: (WelcomeEvent) -> Unit,
    modifier: Modifier = Modifier,
) {
    EScaffold(
        modifier = modifier,
        topBar = { ETopBar(variant = ETopBarVariant.Home, actionIcon = null, onAction = null) },
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
                    icon = EPrevzemIcons.arrowRight(),
                    label = WelcomeStrings.RegisterCta,
                    onClick = { onEvent(WelcomeEvent.RegisterDeviceClicked) },
                    modifier = Modifier.fillMaxWidth(),
                )
                ETextButton(
                    label = WelcomeStrings.HelpLink,
                    icon = EPrevzemIcons.help(),
                    onClick = { onEvent(WelcomeEvent.HelpClicked) },
                )
            }
        },
    ) { _ ->
        EScreen(verticalGap = EPrevzemTheme.spacing.xl) {
            HeroIconStack(modifier = Modifier.fillMaxWidth())

            Column(
                verticalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.sm),
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    text = WelcomeStrings.Title,
                    style = EPrevzemTheme.typography.display,
                    color = EPrevzemTheme.colors.textPrimary,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = WelcomeStrings.Description,
                    style = EPrevzemTheme.typography.body,
                    color = EPrevzemTheme.colors.textSecondary,
                    textAlign = TextAlign.Center,
                )
            }

            EInfoCard(
                value = WelcomeStrings.InfoBody,
                icon = EPrevzemIcons.info(),
                tint = EIconTint.Teal,
            )
        }
    }

    if (state.isHelpVisible) {
        EConfirmationDialog(
            title = WelcomeStrings.HelpTitle,
            message = WelcomeStrings.HelpMessage,
            icon = EPrevzemIcons.help(),
            confirmLabel = WelcomeStrings.HelpClose,
            onConfirm = { onEvent(WelcomeEvent.HelpDismissed) },
            onDismiss = { onEvent(WelcomeEvent.HelpDismissed) },
        )
    }
}

/**
 * Stateful entry point — manages dialog visibility locally so the app root
 * can drop the screen in before a ViewModel layer exists. `onRegisterDeviceClick`
 * is forwarded to the caller; everything else is handled in-screen.
 */
@Composable
fun WelcomeRoute(
    onRegisterDeviceClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var state by remember { mutableStateOf(WelcomeState()) }
    WelcomeScreen(
        state = state,
        modifier = modifier,
        onEvent = { event ->
            when (event) {
                WelcomeEvent.RegisterDeviceClicked -> onRegisterDeviceClick()
                WelcomeEvent.HelpClicked -> state = state.copy(isHelpVisible = true)
                WelcomeEvent.HelpDismissed -> state = state.copy(isHelpVisible = false)
            }
        },
    )
}

/**
 * Hero icon composition for the welcome screen. Three concentric rings
 * (halo → white card disc → primary green disc) carry the security
 * shield, with corner badges for QR / locker semantics. Built from
 * design-system tokens; no hardcoded colours.
 */
@Composable
private fun HeroIconStack(modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    Box(modifier = modifier, contentAlignment = Alignment.Center) {
        Box(
            modifier = Modifier
                .size(192.dp)
                .clip(CircleShape)
                .background(colors.primary50.copy(alpha = 0.55f)),
        )
        Box(
            modifier = Modifier
                .size(148.dp)
                .clip(CircleShape)
                .background(colors.surface)
                .border(1.dp, colors.border, CircleShape),
        )
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .size(104.dp)
                .clip(CircleShape)
                .background(colors.primary),
        ) {
            androidx.compose.material3.Icon(
                painter = EPrevzemIcons.shield(),
                contentDescription = null,
                tint = colors.textOnPrimary,
                modifier = Modifier.size(56.dp),
            )
        }
        HeroBadge(
            icon = EPrevzemIcons.locker(),
            tint = colors.secondary,
            alignment = Alignment.BottomEnd,
            offset = 0.dp to 0.dp,
            sizeDp = 52,
        )
        HeroBadge(
            icon = EPrevzemIcons.qrCode(),
            tint = colors.accent,
            alignment = Alignment.TopStart,
            offset = 0.dp to 0.dp,
            sizeDp = 44,
        )
    }
}

@Composable
private fun HeroBadge(
    icon: androidx.compose.ui.graphics.painter.Painter,
    tint: androidx.compose.ui.graphics.Color,
    alignment: Alignment,
    offset: Pair<androidx.compose.ui.unit.Dp, androidx.compose.ui.unit.Dp>,
    sizeDp: Int,
) {
    val colors = EPrevzemTheme.colors
    Box(
        modifier = Modifier.size(170.dp),
        contentAlignment = alignment,
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset(x = offset.first, y = offset.second)
                .size(sizeDp.dp)
                .clip(CircleShape)
                .background(colors.surface)
                .border(1.dp, colors.border, CircleShape),
        ) {
            androidx.compose.material3.Icon(
                painter = icon,
                contentDescription = null,
                tint = tint,
                modifier = Modifier.size((sizeDp * 0.45f).dp),
            )
        }
    }
}
