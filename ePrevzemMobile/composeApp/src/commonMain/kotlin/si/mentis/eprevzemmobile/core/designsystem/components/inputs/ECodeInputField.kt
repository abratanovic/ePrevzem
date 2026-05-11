package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Read-only display for a code being typed (e.g. 6-digit SMS / device code).
 * The active cell is the next empty one. Input itself is captured by the
 * screen via an invisible [androidx.compose.foundation.text.BasicTextField]
 * that hosts the keyboard; this component renders the visual cells.
 */
@Composable
fun ECodeInputField(
    value: String,
    length: Int = 6,
    label: String? = null,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(8.dp)) {
        if (label != null) {
            Text(text = label, style = EPrevzemTheme.typography.label, color = colors.textPrimary)
        }
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            repeat(length) { index ->
                val char = value.getOrNull(index)
                val active = index == value.length
                val borderColor = when {
                    active -> colors.focus
                    char != null -> colors.primary
                    else -> colors.border
                }
                val fill = if (char != null) colors.primary50 else colors.surface
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(width = 44.dp, height = 56.dp)
                        .clip(EPrevzemTheme.shapes.medium)
                        .background(fill)
                        .border(1.dp, borderColor, EPrevzemTheme.shapes.medium),
                ) {
                    if (char != null) {
                        Text(
                            text = char.toString(),
                            style = EPrevzemTheme.typography.mono.copy(fontSize = 22.sp),
                            color = colors.primary,
                        )
                    }
                }
            }
        }
    }
}
