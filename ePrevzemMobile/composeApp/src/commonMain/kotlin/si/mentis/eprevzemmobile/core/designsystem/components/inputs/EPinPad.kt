package si.mentis.eprevzemmobile.core.designsystem.components.inputs

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Full numeric PIN pad — dot indicator row + 3×4 key grid. Pure display;
 * no internal state. Caller owns [value] and handles all mutations via
 * [onDigit] / [onBackspace].
 */
@Composable
fun EPinPad(
    value: String,
    onDigit: (Int) -> Unit,
    onBackspace: () -> Unit,
    modifier: Modifier = Modifier,
    length: Int = 6,
    onSwitchToFallback: (() -> Unit)? = null,
    switchFallbackLabel: String? = null,
) {
    Column(
        verticalArrangement = Arrangement.spacedBy(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = modifier.fillMaxWidth(),
    ) {
        PinDots(value = value, length = length)
        PinGrid(
            onDigit = onDigit,
            onBackspace = onBackspace,
            onSwitchToFallback = onSwitchToFallback,
            switchFallbackLabel = switchFallbackLabel,
        )
    }
}

@Composable
private fun PinDots(value: String, length: Int) {
    val colors = EPrevzemTheme.colors
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

@Composable
private fun PinGrid(
    onDigit: (Int) -> Unit,
    onBackspace: () -> Unit,
    onSwitchToFallback: (() -> Unit)?,
    switchFallbackLabel: String?,
) {
    val rows = listOf(
        listOf(1, 2, 3),
        listOf(4, 5, 6),
        listOf(7, 8, 9),
    )
    Column(
        verticalArrangement = Arrangement.spacedBy(8.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        rows.forEach { row ->
            Row(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                row.forEach { digit ->
                    DigitKey(digit = digit, onClick = { onDigit(digit) }, modifier = Modifier.weight(1f))
                }
            }
        }
        Row(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (onSwitchToFallback != null && switchFallbackLabel != null) {
                FallbackKey(
                    label = switchFallbackLabel,
                    onClick = onSwitchToFallback,
                    modifier = Modifier.weight(1f),
                )
            } else {
                Box(modifier = Modifier.weight(1f).height(56.dp))
            }
            DigitKey(digit = 0, onClick = { onDigit(0) }, modifier = Modifier.weight(1f))
            BackspaceKey(onClick = onBackspace, modifier = Modifier.weight(1f))
        }
    }
}

@Composable
private fun DigitKey(digit: Int, onClick: () -> Unit, modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .height(56.dp)
            .clip(EPrevzemTheme.shapes.medium)
            .background(colors.surface)
            .border(1.dp, colors.border, EPrevzemTheme.shapes.medium)
            .clickable(onClick = onClick),
    ) {
        Text(
            text = digit.toString(),
            style = typo.section.copy(fontSize = 20.sp, fontWeight = FontWeight.Medium),
            color = colors.textPrimary,
            textAlign = TextAlign.Center,
        )
    }
}

@Composable
private fun BackspaceKey(onClick: () -> Unit, modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .height(56.dp)
            .clip(EPrevzemTheme.shapes.medium)
            .background(colors.surfaceMuted)
            .clickable(onClick = onClick),
    ) {
        Icon(
            painter = EPrevzemIcons.back(),
            contentDescription = "Izbriši",
            tint = colors.textSecondary,
            modifier = Modifier.size(20.dp),
        )
    }
}

@Composable
private fun FallbackKey(label: String, onClick: () -> Unit, modifier: Modifier = Modifier) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .height(56.dp)
            .clip(EPrevzemTheme.shapes.medium)
            .background(colors.surfaceMuted)
            .clickable(onClick = onClick)
            .padding(horizontal = 8.dp),
    ) {
        Text(
            text = label,
            style = typo.caption.copy(fontSize = 11.sp),
            color = colors.textSecondary,
            textAlign = TextAlign.Center,
        )
    }
}
