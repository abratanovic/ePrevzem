package si.mentis.eprevzemmobile.core.designsystem.components.avatar

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Circular identity avatar rendering up to two initials on the Archive Tint
 * (`primary50`) surface with Civic Evergreen text. Used wherever a person needs
 * a visual anchor on a light surface (profile header, account rows). The size is
 * the only knob; the glyph scale and border track it so every avatar reads as the
 * same component at any diameter.
 */
@Composable
fun EAvatar(
    initials: String,
    modifier: Modifier = Modifier,
    size: Dp = 56.dp,
) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(size)
            .clip(CircleShape)
            .background(colors.primary50)
            .border(1.dp, colors.primary100, CircleShape),
    ) {
        Text(
            text = initials.take(2).uppercase(),
            color = colors.primary,
            style = EPrevzemTheme.typography.title.copy(
                fontSize = (size.value * 0.36f).sp,
                fontWeight = FontWeight.SemiBold,
                letterSpacing = 0.sp,
            ),
        )
    }
}

/** Derives at most two uppercase initials from a person's full name. */
fun avatarInitials(fullName: String): String =
    fullName.split(' ')
        .mapNotNull { it.firstOrNull()?.uppercaseChar() }
        .take(2)
        .joinToString("")
