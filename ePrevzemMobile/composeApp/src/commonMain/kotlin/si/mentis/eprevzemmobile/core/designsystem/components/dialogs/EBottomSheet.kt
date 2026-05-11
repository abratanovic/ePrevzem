package si.mentis.eprevzemmobile.core.designsystem.components.dialogs

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.icons.EPrevzemIcons
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * Thin wrapper over Material3's [ModalBottomSheet] that pins the surface
 * colour, the 20 dp top corner radius, and the drag-handle pill from the
 * design system. Callers provide a body composable; the header (title +
 * close button) is rendered by this component when [title] is non-null.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EBottomSheet(
    onDismiss: () -> Unit,
    modifier: Modifier = Modifier,
    title: String? = null,
    content: @Composable () -> Unit,
) {
    val colors = EPrevzemTheme.colors
    val typo = EPrevzemTheme.typography
    val sheetState = rememberModalBottomSheetState()

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState,
        containerColor = colors.surface,
        shape = EPrevzemTheme.shapes.bottomSheet,
        dragHandle = {
            Box(
                modifier = Modifier
                    .padding(top = 12.dp, bottom = 4.dp)
                    .size(width = 36.dp, height = 4.dp)
                    .clip(EPrevzemTheme.shapes.pill)
                    .background(colors.divider),
            )
        },
        modifier = modifier,
    ) {
        Column(
            verticalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 20.dp, vertical = 16.dp),
        ) {
            if (title != null) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text(text = title, style = typo.section, color = colors.textPrimary)
                    Icon(
                        painter = EPrevzemIcons.close(),
                        contentDescription = "Zapri",
                        tint = colors.textSecondary,
                        modifier = Modifier
                            .size(22.dp)
                            .clickable(onClick = onDismiss),
                    )
                }
            }
            content()
        }
    }
}
