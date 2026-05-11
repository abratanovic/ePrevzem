package si.mentis.eprevzemmobile.core.designsystem.components.buttons

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private val ButtonHeight = 48.dp

@Composable
fun EPrimaryButton(
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: Painter? = null,
    enabled: Boolean = true,
    loading: Boolean = false,
) {
    val colors = EPrevzemTheme.colors
    Button(
        onClick = onClick,
        modifier = modifier.heightIn(min = ButtonHeight),
        enabled = enabled && !loading,
        shape = EPrevzemTheme.shapes.button,
        colors = ButtonDefaults.buttonColors(
            containerColor = colors.primary,
            contentColor = colors.textOnPrimary,
            disabledContainerColor = colors.disabledBg,
            disabledContentColor = colors.disabledFg,
        ),
        contentPadding = ButtonDefaults.ContentPadding,
    ) {
        if (loading) {
            CircularProgressIndicator(
                modifier = Modifier.size(18.dp),
                color = colors.textOnPrimary,
                strokeWidth = 2.dp,
            )
        } else {
            ButtonContent(label = label, icon = icon)
        }
    }
}

@Composable
fun ESecondaryButton(
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: Painter? = null,
    enabled: Boolean = true,
) {
    val colors = EPrevzemTheme.colors
    OutlinedButton(
        onClick = onClick,
        modifier = modifier.heightIn(min = ButtonHeight),
        enabled = enabled,
        shape = EPrevzemTheme.shapes.button,
        border = BorderStroke(1.dp, if (enabled) colors.primary else colors.disabledFg),
        colors = ButtonDefaults.outlinedButtonColors(
            containerColor = colors.surface,
            contentColor = colors.primary,
            disabledContainerColor = colors.surface,
            disabledContentColor = colors.disabledFg,
        ),
    ) {
        ButtonContent(label = label, icon = icon)
    }
}

@Composable
fun ETextButton(
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: Painter? = null,
    enabled: Boolean = true,
) {
    val colors = EPrevzemTheme.colors
    TextButton(
        onClick = onClick,
        modifier = modifier,
        enabled = enabled,
        colors = ButtonDefaults.textButtonColors(
            contentColor = colors.primary,
            disabledContentColor = colors.disabledFg,
        ),
    ) {
        ButtonContent(label = label, icon = icon)
    }
}

@Composable
fun EDestructiveButton(
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: Painter? = null,
    enabled: Boolean = true,
) {
    val colors = EPrevzemTheme.colors
    Button(
        onClick = onClick,
        modifier = modifier.heightIn(min = ButtonHeight),
        enabled = enabled,
        shape = EPrevzemTheme.shapes.button,
        colors = ButtonDefaults.buttonColors(
            containerColor = colors.error,
            contentColor = colors.textOnPrimary,
            disabledContainerColor = colors.disabledBg,
            disabledContentColor = colors.disabledFg,
        ),
    ) {
        ButtonContent(label = label, icon = icon)
    }
}

@Composable
private fun ButtonContent(label: String, icon: Painter?) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        if (icon != null) {
            Icon(
                painter = icon,
                contentDescription = null,
                modifier = Modifier.size(18.dp),
            )
            Box(Modifier.size(width = 8.dp, height = 1.dp))
        }
        Text(text = label, style = EPrevzemTheme.typography.button)
    }
}
