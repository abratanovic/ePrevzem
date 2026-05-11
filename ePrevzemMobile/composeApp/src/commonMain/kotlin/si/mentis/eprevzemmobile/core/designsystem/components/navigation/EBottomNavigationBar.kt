package si.mentis.eprevzemmobile.core.designsystem.components.navigation

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

@Immutable
data class EBottomNavItem(
    val id: String,
    val icon: ImageVector,
    val label: String,
    val primary: Boolean = false,
)

@Composable
fun EBottomNavigationBar(
    items: List<EBottomNavItem>,
    activeId: String,
    onSelect: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    val colors = EPrevzemTheme.colors
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceAround,
        modifier = modifier
            .fillMaxWidth()
            .background(colors.surface)
            .border(width = 1.dp, color = colors.border, shape = androidx.compose.foundation.shape.RoundedCornerShape(0.dp))
            .padding(horizontal = 8.dp, vertical = 8.dp),
    ) {
        items.forEach { item ->
            if (item.primary) {
                PrimaryTab(item = item, onClick = { onSelect(item.id) })
            } else {
                Tab(item = item, active = item.id == activeId, onClick = { onSelect(item.id) })
            }
        }
    }
}

@Composable
private fun Tab(item: EBottomNavItem, active: Boolean, onClick: () -> Unit) {
    val colors = EPrevzemTheme.colors
    val tint = if (active) colors.primary else colors.textMuted
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(4.dp),
        modifier = Modifier.clickable(onClick = onClick).padding(horizontal = 16.dp, vertical = 8.dp),
    ) {
        Icon(imageVector = item.icon, contentDescription = item.label, tint = tint, modifier = Modifier.size(22.dp))
        Text(
            text = item.label,
            style = EPrevzemTheme.typography.caption.copy(
                fontWeight = if (active) FontWeight.SemiBold else FontWeight.Medium,
            ),
            color = tint,
        )
    }
}

@Composable
private fun PrimaryTab(item: EBottomNavItem, onClick: () -> Unit) {
    val colors = EPrevzemTheme.colors
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .offset(y = (-20).dp)
            .size(56.dp)
            .clip(CircleShape)
            .background(colors.primary)
            .clickable(onClick = onClick),
    ) {
        Icon(imageVector = item.icon, contentDescription = item.label, tint = Color.White, modifier = Modifier.size(26.dp))
    }
}
