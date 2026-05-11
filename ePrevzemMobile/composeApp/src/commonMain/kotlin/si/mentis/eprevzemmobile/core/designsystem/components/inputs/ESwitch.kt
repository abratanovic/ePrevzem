package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.selection.toggleable
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun ESwitch(
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
) {
    val colors = EPrevzemTheme.colors
    val trackColor = when {
        !enabled -> colors.disabledBg
        checked -> colors.primary
        else -> colors.surfaceSunken
    }
    val borderColor = when {
        !enabled -> colors.disabledFg
        checked -> colors.primary
        else -> colors.border
    }

    Box(
        modifier = modifier
            .size(width = 48.dp, height = 28.dp)
            .clip(EPrevzemTheme.shapes.pill)
            .background(trackColor)
            .border(1.dp, borderColor, EPrevzemTheme.shapes.pill)
            .toggleable(
                value = checked,
                enabled = enabled,
                role = Role.Switch,
                onValueChange = onCheckedChange,
            ),
    ) {
        Surface(
            color = colors.surface,
            shadowElevation = 2.dp,
            shape = EPrevzemTheme.shapes.pill,
            modifier = Modifier
                .offset(x = if (checked) 23.dp else 3.dp, y = 3.dp)
                .size(22.dp),
            content = {},
        )
    }
}
