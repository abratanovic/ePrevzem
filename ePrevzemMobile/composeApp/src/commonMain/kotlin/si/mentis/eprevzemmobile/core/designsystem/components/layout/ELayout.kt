package si.mentis.eprevzemmobile.core.designsystem.components.layout

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import si.mentis.eprevzemmobile.core.designsystem.components.buttons.ETextButton
import si.mentis.eprevzemmobile.core.designsystem.theme.EPrevzemTheme

/**
 * App-wide scaffold: paints the warm off-white background, wires up
 * safe-area insets, and slots an optional top bar and bottom bar without
 * pulling in Material3's `Scaffold` (which assumes Material colour roles).
 */
@Composable
fun EScaffold(
    modifier: Modifier = Modifier,
    topBar: @Composable () -> Unit = {},
    bottomBar: @Composable () -> Unit = {},
    content: @Composable (PaddingValues) -> Unit,
) {
    val colors = EPrevzemTheme.colors
    Column(
        modifier = modifier
            .fillMaxSize()
            .background(colors.background),
    ) {
        topBar()
        Box(modifier = Modifier.weight(1f)) {
            content(PaddingValues(0.dp))
        }
        Box(modifier = Modifier.navigationBarsPadding()) {
            bottomBar()
        }
    }
}

/**
 * Vertical-scrolling screen body with the standard 24 dp horizontal padding
 * and 24 dp vertical section gap. Use inside [EScaffold] for typical app
 * screens.
 */
@Composable
fun EScreen(
    modifier: Modifier = Modifier,
    horizontalPadding: Dp = EPrevzemTheme.spacing.screenHorizontal,
    verticalGap: Dp = EPrevzemTheme.spacing.sectionGap,
    scrollable: Boolean = true,
    content: @Composable () -> Unit,
) {
    val column: @Composable () -> Unit = {
        Column(
            verticalArrangement = Arrangement.spacedBy(verticalGap),
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = horizontalPadding, vertical = 16.dp),
        ) { content() }
    }
    if (scrollable) {
        Column(
            modifier = modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState()),
        ) { column() }
    } else {
        Box(modifier = modifier) { column() }
    }
}

@Composable
fun ESectionHeader(
    title: String,
    modifier: Modifier = Modifier,
    actionLabel: String? = null,
    onAction: (() -> Unit)? = null,
) {
    val colors = EPrevzemTheme.colors
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween,
        modifier = modifier.fillMaxWidth().padding(bottom = 8.dp),
    ) {
        Text(text = title, style = EPrevzemTheme.typography.section, color = colors.textPrimary)
        if (actionLabel != null && onAction != null) {
            ETextButton(label = actionLabel, onClick = onAction)
        }
    }
}

@Composable
fun EDivider(
    modifier: Modifier = Modifier,
    thickness: Dp = 1.dp,
) {
    val colors = EPrevzemTheme.colors
    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(thickness)
            .background(colors.divider),
    )
}
