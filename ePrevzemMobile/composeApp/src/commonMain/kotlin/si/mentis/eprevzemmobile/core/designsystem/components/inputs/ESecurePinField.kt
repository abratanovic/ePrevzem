package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Icon
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

private const val MaxPinLength = 6

@Composable
fun ESecurePinField(
    value: String,
    onValueChange: (String) -> Unit,
    label: String,
    visible: Boolean,
    onVisibilityToggle: () -> Unit,
    modifier: Modifier = Modifier,
    isError: Boolean = false,
    enabled: Boolean = true,
) {
    val colors = EPrevzemTheme.colors
    val spacing = EPrevzemTheme.spacing
    var focused by remember { mutableStateOf(false) }
    val borderColor = when {
        isError -> colors.error
        focused -> colors.focus
        else -> colors.border
    }

    Column(
        verticalArrangement = Arrangement.spacedBy(6.dp),
        modifier = modifier,
    ) {
        Text(
            text = label,
            style = EPrevzemTheme.typography.label,
            color = colors.textPrimary,
        )
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(spacing.sm),
            modifier = Modifier
                .fillMaxWidth()
                .heightIn(min = 52.dp)
                .clip(EPrevzemTheme.shapes.medium)
                .background(if (enabled) colors.surface else colors.disabledBg)
                .border(1.dp, borderColor, EPrevzemTheme.shapes.medium)
                .padding(start = 16.dp, end = 8.dp, top = 8.dp, bottom = 8.dp),
        ) {
            Box(modifier = Modifier.weight(1f)) {
                if (value.isEmpty()) {
                    Text(
                        text = "......",
                        style = EPrevzemTheme.typography.mono.copy(
                            fontSize = 18.sp,
                            lineHeight = 22.sp,
                            letterSpacing = 6.sp,
                            fontWeight = FontWeight.SemiBold,
                        ),
                        color = colors.disabledFg,
                    )
                }
                BasicTextField(
                    value = value,
                    onValueChange = { raw ->
                        onValueChange(raw.filter { it.isDigit() }.take(MaxPinLength))
                    },
                    enabled = enabled,
                    singleLine = true,
                    cursorBrush = SolidColor(colors.primary),
                    visualTransformation = if (visible) {
                        VisualTransformation.None
                    } else {
                        PasswordVisualTransformation()
                    },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
                    textStyle = EPrevzemTheme.typography.mono.copy(
                        fontSize = 18.sp,
                        lineHeight = 22.sp,
                        letterSpacing = if (visible) 3.sp else 6.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = colors.textPrimary,
                    ),
                    modifier = Modifier
                        .fillMaxWidth()
                        .onFocusChanged { focused = it.isFocused },
                )
            }
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(36.dp)
                    .clip(CircleShape)
                    .clickable(enabled = enabled, onClick = onVisibilityToggle),
            ) {
                Icon(
                    painter = if (visible) EPrevzemIcons.visibilityOff() else EPrevzemIcons.visibility(),
                    contentDescription = if (visible) "Skrij PIN" else "Pokaži PIN",
                    tint = colors.textSecondary,
                    modifier = Modifier.size(20.dp),
                )
            }
        }
    }
}
