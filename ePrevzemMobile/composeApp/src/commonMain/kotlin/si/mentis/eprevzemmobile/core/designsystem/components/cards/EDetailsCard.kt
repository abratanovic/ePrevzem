package si.mentis.eprevzemmobile.core.designsystem.components.cards

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Composable
fun EDetailsSectionLabel(
    title: String,
    modifier: Modifier = Modifier,
    hint: (@Composable () -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween,
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp),
    ) {
        Text(
            text = title.uppercase(),
            style = EPrevzemTheme.typography.caption.copy(
                fontWeight = FontWeight.SemiBold,
                letterSpacing = 1.0.sp,
            ),
            color = colors.textMuted,
        )
        if (hint != null) {
            hint()
        }
    }
}

@Composable
fun EDetailsCard(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit,
) {
    val spacing = EPrevzemTheme.spacing
    Column(
        verticalArrangement = Arrangement.spacedBy(14.dp),
        modifier = cardModifier(modifier.fillMaxWidth())
            .padding(spacing.cardInternal),
    ) {
        content()
    }
}

@Composable
fun EDetailsRow(
    icon: Painter,
    label: String,
    value: String,
    modifier: Modifier = Modifier,
    tint: EIconTint = EIconTint.Green,
    mono: Boolean = false,
    trailing: (@Composable () -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val valueStyle = if (mono) {
        typo.mono.copy(
            fontSize = 15.sp,
            lineHeight = 20.sp,
            fontWeight = FontWeight.SemiBold,
            color = colors.textPrimary,
        )
    } else {
        typo.body.copy(
            fontSize = 16.sp,
            lineHeight = 21.sp,
            fontWeight = FontWeight.SemiBold,
            color = colors.textPrimary,
        )
    }

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(EPrevzemTheme.spacing.sm),
        modifier = modifier.fillMaxWidth(),
    ) {
        EIconChip(painter = icon, tint = tint)
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = label.uppercase(),
                style = typo.caption.copy(fontWeight = FontWeight.Medium),
                color = colors.textMuted,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            Text(
                text = value,
                style = valueStyle,
                color = colors.textPrimary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
        }
        if (trailing != null) {
            trailing()
        }
    }
}

@Composable
fun EDetailsDivider(modifier: Modifier = Modifier) {
    Box(
        modifier = modifier
            .fillMaxWidth()
            .padding(start = 56.dp)
            .height(1.dp)
            .background(EPrevzemTheme.colors.divider),
    )
}
