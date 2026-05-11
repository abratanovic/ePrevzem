package si.mentis.eprevzemmobile.core.designsystem.components.dialogs

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EDestructiveButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.EPrimaryButton
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ESecondaryButton
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun EConfirmationDialog(
    title: String,
    onDismiss: () -> Unit,
    onConfirm: () -> Unit,
    modifier: Modifier = Modifier,
    message: String? = null,
    icon: Painter = EPrevzemIcons.help(),
    confirmLabel: String = "Potrdi",
    dismissLabel: String = "",
    destructive: Boolean = false,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    Dialog(onDismissRequest = onDismiss, properties = DialogProperties()) {
        Column(
            verticalArrangement = Arrangement.spacedBy(16.dp),
            modifier = modifier
                .widthIn(max = 320.dp)
                .clip(EPrevzemTheme.shapes.extraLarge)
                .background(colors.surface)
                .padding(24.dp),
        ) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(56.dp)
                        .clip(CircleShape)
                        .background(if (destructive) colors.errorBg else colors.primary50),
                ) {
                    Icon(
                        painter = icon,
                        contentDescription = null,
                        tint = if (destructive) colors.error else colors.primary,
                        modifier = Modifier.size(28.dp),
                    )
                }
                Text(text = title, style = typo.section, color = colors.textPrimary, textAlign = TextAlign.Center)
                if (message != null) {
                    Text(
                        text = message,
                        style = typo.bodySmall,
                        color = colors.textSecondary,
                        textAlign = TextAlign.Center,
                    )
                }
            }
            Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                if (destructive) {
                    EDestructiveButton(label = confirmLabel, onClick = onConfirm, modifier = Modifier.fillMaxWidth())
                } else {
                    EPrimaryButton(label = confirmLabel, onClick = onConfirm, modifier = Modifier.fillMaxWidth())
                }
                if (dismissLabel.isNotEmpty()) {
                    ESecondaryButton(label = dismissLabel, onClick = onDismiss, modifier = Modifier.fillMaxWidth())
                }
            }
        }
    }
}
