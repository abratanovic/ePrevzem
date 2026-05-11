package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.border
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Icon
import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun ETextField(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    label: String? = null,
    placeholder: String? = null,
    leadingIcon: ImageVector? = null,
    helper: String? = null,
    error: String? = null,
    enabled: Boolean = true,
    singleLine: Boolean = true,
    visualTransformation: VisualTransformation = VisualTransformation.None,
    keyboardType: KeyboardType = KeyboardType.Text,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    val radius = EPrevzemTheme.radius
    var focused by remember { mutableStateOf(false) }

    val borderColor = when {
        error != null -> colors.error
        focused -> colors.focus
        else -> colors.border
    }

    Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(6.dp)) {
        if (label != null) {
            Text(
                text = label,
                style = EPrevzemTheme.typography.label,
                color = colors.textPrimary,
            )
        }
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
            modifier = Modifier
                .fillMaxWidth()
                .heightIn(min = 52.dp)
                .clip(EPrevzemTheme.shapes.medium)
                .background(if (enabled) colors.surface else colors.disabledBg)
                .border(1.dp, borderColor, EPrevzemTheme.shapes.medium)
                .padding(horizontal = 14.dp, vertical = 8.dp),
        ) {
            if (leadingIcon != null) {
                Icon(
                    imageVector = leadingIcon,
                    contentDescription = null,
                    tint = colors.textMuted,
                    modifier = Modifier.size(18.dp),
                )
            }
            Box(modifier = Modifier.weight(1f)) {
                if (value.isEmpty() && placeholder != null) {
                    Text(
                        text = placeholder,
                        style = EPrevzemTheme.typography.body,
                        color = colors.textMuted,
                    )
                }
                BasicTextField(
                    value = value,
                    onValueChange = onValueChange,
                    enabled = enabled,
                    singleLine = singleLine,
                    cursorBrush = SolidColor(colors.primary),
                    visualTransformation = visualTransformation,
                    keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
                    textStyle = EPrevzemTheme.typography.body.copy(color = colors.textPrimary),
                    modifier = Modifier
                        .fillMaxWidth()
                        .onFocusChanged { focused = it.isFocused },
                )
            }
        }
        when {
            error != null -> Text(
                text = error,
                style = EPrevzemTheme.typography.caption,
                color = colors.error,
            )
            helper != null -> Text(
                text = helper,
                style = EPrevzemTheme.typography.caption,
                color = colors.textMuted,
            )
        }
    }
}

