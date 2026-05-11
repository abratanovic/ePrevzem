package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Visual representation of a PIN code being typed. Dots fill as digits are
 * entered. Input capture is the caller's responsibility (typically a hidden
 * BasicTextField with [androidx.compose.ui.text.input.KeyboardType.NumberPassword]).
 */
@Composable
fun EPinInputField(
    value: String,
    length: Int = 4,
    label: String? = null,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(8.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        if (label != null) {
            Text(text = label, style = EPrevzemTheme.typography.label, color = colors.textPrimary)
        }
        Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
            repeat(length) { index ->
                val filled = index < value.length
                Box(
                    modifier = Modifier
                        .size(16.dp)
                        .background(
                            color = if (filled) colors.primary else Color.Transparent,
                            shape = CircleShape,
                        )
                        .border(
                            width = 1.5.dp,
                            color = if (filled) colors.primary else colors.border,
                            shape = CircleShape,
                        ),
                )
            }
        }
    }
}
