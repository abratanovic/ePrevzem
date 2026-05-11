package si.mentis.eprevzemmobile.core.designsystem.components.navigation

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.clickable
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

enum class ETopBarVariant { Home, Detail, Plain }

@Composable
fun ETopBar(
    variant: ETopBarVariant,
    modifier: Modifier = Modifier,
    title: String? = null,
    eyebrow: String? = "Republika Slovenija",
    appName: String = "ePrevzem",
    onBack: (() -> Unit)? = null,
    actionIcon: ImageVector? = EPrevzemIcons.Settings,
    onAction: (() -> Unit)? = null,
    notificationCount: Int = 0,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography

    Box(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(bottomStart = 24.dp, bottomEnd = 24.dp))
            .background(colors.primary)
            .padding(start = 20.dp, end = 20.dp, top = 16.dp, bottom = 28.dp),
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.fillMaxWidth().heightIn(min = 40.dp),
        ) {
            if (variant == ETopBarVariant.Detail && onBack != null) {
                TopBarCircleButton(icon = EPrevzemIcons.Back, onClick = onBack)
            }
            Box(modifier = Modifier.weight(1f)) {
                when (variant) {
                    ETopBarVariant.Home -> Column {
                        if (eyebrow != null) {
                            Text(
                                text = eyebrow.uppercase(),
                                style = typo.caption.copy(letterSpacing = 1.0.sp, fontSize = 11.sp),
                                color = Color.White.copy(alpha = 0.7f),
                            )
                        }
                        Text(
                            text = appName,
                            style = typo.section.copy(fontSize = 18.sp, fontWeight = FontWeight.Bold),
                            color = Color.White,
                        )
                    }
                    ETopBarVariant.Detail, ETopBarVariant.Plain -> {
                        if (title != null) {
                            Text(
                                text = title,
                                style = typo.section.copy(fontSize = 17.sp, fontWeight = FontWeight.Bold),
                                color = Color.White,
                            )
                        }
                    }
                }
            }
            if (variant != ETopBarVariant.Plain && actionIcon != null && onAction != null) {
                Box {
                    TopBarCircleButton(icon = actionIcon, onClick = onAction)
                    if (notificationCount > 0) {
                        NotificationDot(count = notificationCount)
                    }
                }
            }
        }
    }
}

@Composable
private fun TopBarCircleButton(icon: ImageVector, onClick: () -> Unit) {
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(36.dp)
            .clip(CircleShape)
            .background(Color.White.copy(alpha = 0.12f))
            .clickable(onClick = onClick),
    ) {
        Icon(imageVector = icon, contentDescription = null, tint = Color.White, modifier = Modifier.size(20.dp))
    }
}

@Composable
private fun NotificationDot(count: Int) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(18.dp)
            .clip(CircleShape)
            .background(colors.error)
            .padding(horizontal = 4.dp),
    ) {
        Text(
            text = if (count > 99) "99+" else count.toString(),
            style = EPrevzemTheme.typography.caption.copy(fontSize = 11.sp, fontWeight = FontWeight.Bold),
            color = Color.White,
        )
    }
}
