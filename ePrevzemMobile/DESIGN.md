---
name: ePrevzem
description: Secure Slovenian government document pickup and operator locker workflows.
colors:
  primary: "#0F5132"
  primary-dark: "#0A3A24"
  primary-light: "#1B7A4C"
  primary-50: "#E8F1EC"
  primary-100: "#C8DDD2"
  secondary: "#5C8A9E"
  secondary-light: "#B8D4DD"
  secondary-50: "#EAF1F4"
  accent: "#B8862A"
  accent-light: "#F1E4C2"
  background: "#F7F5F0"
  surface: "#FFFFFF"
  surface-muted: "#F0EEE8"
  surface-sunken: "#ECEAE3"
  text-primary: "#1A2330"
  text-secondary: "#4A5568"
  text-muted: "#8A94A3"
  text-on-primary: "#FFFFFF"
  text-link: "#0F5132"
  border: "#E2E0D8"
  divider: "#ECEAE3"
  focus: "#1B7A4C"
  success: "#2E7D5B"
  success-bg: "#E5F1EB"
  warning: "#C77B1F"
  warning-bg: "#FBEFDC"
  error: "#B33A3A"
  error-bg: "#F8E5E5"
  info: "#2D6CB0"
  info-bg: "#E3ECF6"
  disabled-bg: "#E8E6DF"
  disabled-fg: "#B0ADA3"
typography:
  display:
    fontFamily: "Inter Variable, Inter, system-ui, sans-serif"
    fontSize: "32px"
    fontWeight: 700
    lineHeight: "38px"
    letterSpacing: "-0.32px"
  headline:
    fontFamily: "Inter Variable, Inter, system-ui, sans-serif"
    fontSize: "24px"
    fontWeight: 700
    lineHeight: "30px"
    letterSpacing: "-0.24px"
  title:
    fontFamily: "Inter Variable, Inter, system-ui, sans-serif"
    fontSize: "18px"
    fontWeight: 600
    lineHeight: "24px"
    letterSpacing: "0px"
  body:
    fontFamily: "Inter Variable, Inter, system-ui, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: "22px"
    letterSpacing: "0px"
  label:
    fontFamily: "Inter Variable, Inter, system-ui, sans-serif"
    fontSize: "13px"
    fontWeight: 600
    lineHeight: "16px"
    letterSpacing: "0px"
  mono:
    fontFamily: "JetBrains Mono Variable, JetBrains Mono, ui-monospace, monospace"
    fontSize: "13px"
    fontWeight: 500
    lineHeight: "18px"
    letterSpacing: "0px"
rounded:
  sm: "6px"
  md: "10px"
  lg: "14px"
  xl: "20px"
  button: "12px"
  pill: "999px"
spacing:
  xxs: "4px"
  xs: "8px"
  sm: "12px"
  md: "16px"
  lg: "20px"
  xl: "24px"
  xxl: "32px"
  screen-horizontal: "24px"
  card-internal: "16px"
  touch-target: "44px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-on-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "12px 16px"
    height: "48px"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.primary}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "12px 16px"
    height: "48px"
  input-default:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "52px"
  chip-status:
    backgroundColor: "{colors.success-bg}"
    textColor: "{colors.success}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "6px 12px"
  nav-bottom:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-muted}"
    typography: "{typography.label}"
    padding: "8px 8px"
  topbar-home:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-on-primary}"
    typography: "{typography.title}"
    padding: "16px 20px 28px"
---

# Design System: ePrevzem

## 1. Overview

**Creative North Star: "The Civic Ledger"**

This system is built for a real government service, not a startup product wearing institutional colors. The visual language is formal, calm, and traceable. Every screen should feel like it belongs to a production Slovenian public-service app where users are handling sensitive, official workflows and expect the interface to behave predictably.

The atmosphere is warm-light rather than sterile-white. Surfaces read like organized paper, not glossy dashboards. Green is the authority color, used as the primary signal for action, confirmation, and trust. Supporting blue-grey and restrained gold widen the system without turning it into a multi-accent consumer palette.

This system explicitly rejects startup gradients, card-heavy dashboards, playful fintech styling, and glossy marketing UI. It also rejects generic SaaS neutrality. The app should feel civic, polished, and task-first, with enough warmth to stay human.

**Key Characteristics:**
- restrained government-grade color, with one authoritative primary green
- warm paper-like backgrounds instead of cold white app chrome
- compact, disciplined typography built on one sans family and one mono utility face
- soft-radius controls and bordered surfaces, not floating glossy panels
- status-rich components that communicate process state clearly

## 2. Colors

The palette is restrained and stateful: one trusted green leads, muted neutrals carry the surface, and semantic colors speak only when status requires it.

### Primary
- **Civic Evergreen** (`#0F5132`): the system’s authority color, used for primary actions, top-bar backgrounds, active navigation states, links, focus cursors, and trust-bearing confirmations.
- **Registry Pine** (`#0A3A24`): the pressed, deeper authority green, used when the primary color needs more weight in dense or contrast-sensitive contexts.
- **Signal Green** (`#1B7A4C`): the lighter active-state green, used for focus and interactive lift.
- **Archive Tint** (`#E8F1EC`): the soft green wash behind icon chips, primary-tinted surfaces, and low-emphasis supportive highlights.
- **Archive Soft** (`#C8DDD2`): the denser green tint, used when a supportive primary surface needs more presence without becoming a button.

### Secondary
- **Administrative Slate** (`#5C8A9E`): the supporting cool accent, used sparingly for secondary icon treatment and informational support states.
- **Administrative Mist** (`#B8D4DD`): the softened blue-grey companion, used as a supportive tint.
- **Administrative Wash** (`#EAF1F4`): the pale blue-grey background tint for secondary chips and icon holders.

### Tertiary
- **Protocol Gold** (`#B8862A`): a controlled ceremonial accent, used for badges, highlights, and rare emphasis where the system needs official gravitas rather than urgency.
- **Protocol Wash** (`#F1E4C2`): the matching pale gold surface tint for icon chips and restrained accent support.

### Neutral
- **Warm Paper** (`#F7F5F0`): the application background. This is the key neutral. It keeps the app from feeling like a generic white dashboard.
- **Service White** (`#FFFFFF`): the main content surface for cards, fields, and sheets.
- **Filed Surface** (`#F0EEE8`): the muted neutral layer for secondary controls, chips, and supporting surfaces.
- **Inset Surface** (`#ECEAE3`): the sunken neutral for separators, recessed states, and flat depth cues.
- **Government Ink** (`#1A2330`): primary reading text, used for titles, labels that must carry weight, and default body copy.
- **Case Note** (`#4A5568`): secondary explanatory text, metadata, and less critical copy.
- **Quiet Ledger** (`#8A94A3`): muted text, captions, placeholders, and inactive icon treatment.
- **Paper Edge** (`#E2E0D8`): the default border color for fields, cards, and low-key separators.
- **Soft Divider** (`#ECEAE3`): lightweight internal dividers and rule lines.

### Named Rules
**The One Authority Rule.** Civic Evergreen is the only everyday accent. Secondary blue-grey and protocol gold are support colors, not competing calls to action.

**The Warm Paper Rule.** Default page backgrounds use Warm Paper, not pure white. If a screen starts to feel like generic SaaS, the background is too cold.

**The State Speaks Rule.** Success, warning, error, and info colors belong to status communication only. They are forbidden as decorative accents.

## 3. Typography

**Display Font:** Inter Variable, Inter, system-ui, sans-serif  
**Body Font:** Inter Variable, Inter, system-ui, sans-serif  
**Label/Mono Font:** JetBrains Mono Variable, JetBrains Mono, ui-monospace, monospace

**Character:** one disciplined sans does nearly all the work. It keeps the product native-feeling, sober, and efficient. Mono is reserved for PIN and machine-like data moments where numeric trust matters.

### Hierarchy
- **Display** (`700`, `32px`, `38px`, `-0.32px`): for page-level welcomes and high-importance screen headings. It is bold, compact, and never theatrical.
- **Headline** (`700`, `24px`, `30px`, `-0.24px`): for major section and route titles where the UI needs strong structure without hero behavior.
- **Title** (`600`, `18px`, `24px`): for top bars, section headers, and high-value component titles.
- **Body** (`400`, `15px`, `22px`): the default reading style for app copy, helper text, and screen content. Use disciplined lengths and avoid bloated paragraphs.
- **Label** (`600`, `13px`, `16px`): for field labels, compact UI labels, and chip text where confidence matters more than scale.
- **Mono** (`500`, `13px`, `18px`): for PIN input, code-like values, and structured strings that benefit from fixed-width rhythm.

### Named Rules
**The No Display in Controls Rule.** Display and Headline styles are forbidden in buttons, fields, chips, and dense navigation. Product UI earns trust through restraint, not dramatics.

**The Utility Sans Rule.** Inter carries the interface because the interface is the tool. Font personality comes from weight, spacing, and color discipline, not from rotating families.

## 4. Elevation

This system is flat by default. Depth is conveyed primarily through warm surface separation, borders, and tonal contrast, not through visible shadows. The nominal elevation scale exists (`0dp`, `1dp`, `4dp`, `12dp`), but the visual language behaves as a layered paper system rather than a floating card system.

### Shadow Vocabulary
- **Resting Surface** (`0dp`): default screens, form surfaces, navigation bars, and most cards. The border and tonal separation do the work.
- **Card Lift** (`1dp`): reserved for components that need a slight material cue without looking detached from the page.
- **Elevated Utility** (`4dp`): used when a control or utility layer must temporarily separate from the base plane.
- **Modal Layer** (`12dp`): for sheets and confirmation surfaces that must clearly sit above the task.

### Named Rules
**The Flat-by-Default Rule.** If a component can be understood with border, radius, and surface contrast, shadow is prohibited.

**The 2014 Test.** If the UI starts to look like an old mobile app with visible floating cards, the shadow is too heavy or the border is too weak.

## 5. Components

### Buttons
- **Character:** quiet authority. Buttons should feel official, stable, and ready for confirmation-heavy flows.
- **Shape:** softly squared corners (`12px radius`) with a minimum touch height of `48px`.
- **Primary:** Civic Evergreen background (`#0F5132`) with white text, using semi-bold body button type and standard Material padding.
- **Secondary:** white surface with `1px` Civic Evergreen border and green text. This is for alternate actions, not for weak actions.
- **Destructive:** solid error red background with white text. Red is reserved for explicit destructive intent.
- **Hover / Focus:** state change should come from darker green, sharper border clarity, or clearer focus color, not from ornamental animation.

### Chips
- **Character:** compact status tokens, never decorative pills.
- **Shape:** full pill rounding (`999px`) with `6px 12px` padding.
- **Status Vocabulary:** each chip pairs tinted background, matching icon, and same-hue text. Ready uses success green, Expiring uses warning amber, PickedUp uses info blue, Expired uses error red, Draft uses muted neutral.
- **Role:** chips communicate process state only. They are not filters masquerading as status.

### Cards / Containers
- **Character:** bordered paper modules, not floating dashboard tiles.
- **Corner Style:** large card radius (`14px`) for primary cards, medium radius (`10px`) for tighter utility containers.
- **Background:** Service White on Warm Paper background.
- **Shadow Strategy:** border-led, with tonal separation doing most of the depth work.
- **Border:** `1px` Paper Edge (`#E2E0D8`) by default.
- **Internal Padding:** compact and generous enough for touch, typically `16px` to `18px`.
- **Internal Structure:** dividers use Soft Divider (`#ECEAE3`) and metadata labels use Quiet Ledger.

### Inputs / Fields
- **Character:** form controls should feel precise and administrative, never soft-consumer or glossy.
- **Style:** white background, `1px` border, medium radius (`10px`), minimum height `52px`, horizontal padding around `14px` to `16px`.
- **Focus:** border shifts to Signal Green (`#1B7A4C`), cursor uses Civic Evergreen, and the structure stays stable. Focus is clear, not loud.
- **Error / Disabled:** error uses the error border immediately. Disabled swaps to disabled neutrals without changing geometry.
- **Clear / Visibility Actions:** compact circular utility buttons sit inside the field, using muted surface treatment instead of heavy icon buttons.

### Navigation
- **Top Bar:** drenched Civic Evergreen with white text, bottom padding that gives the bar presence, and circular translucent utility controls or initials. It should feel official and anchored.
- **Bottom Navigation:** white bar with `1px` border, compact icon-plus-label tabs, and primary green used only for the active state or floating primary tab.
- **State:** inactive navigation uses Quiet Ledger. Active navigation uses Civic Evergreen. No inactive accent color is allowed.

### Signature Component
- **Pickup Card:** the pickup card is the system’s signature content block. It combines icon chip, title, metadata, status pill, location and deadline structure, plus a full-width warning strip when timing risk exists. It should feel like an organized government case summary, not a consumer content card.

## 6. Do's and Don'ts

### Do:
- **Do** use Warm Paper (`#F7F5F0`) as the default page background so the app feels civic and grounded rather than generic white SaaS.
- **Do** reserve Civic Evergreen (`#0F5132`) for primary action, active state, and trust-bearing emphasis.
- **Do** keep fields, cards, banners, and chips on one geometric family: soft corners, clear borders, disciplined padding.
- **Do** communicate status through the semantic palette with matching tinted backgrounds, icons, and text.
- **Do** keep typography compact and operational, with one sans family carrying most of the interface.
- **Do** design for WCAG AA, including contrast, touch targets, focus visibility, and readable hierarchy in every state.

### Don't:
- **Don't** use startup gradients or attention-seeking visual effects anywhere in the product.
- **Don't** build card-heavy dashboards that feel like generic business SaaS.
- **Don't** borrow playful fintech styling, including flashy accents, over-friendly UI tone, or ornamental polish.
- **Don't** use glossy marketing UI patterns that prioritize presentation over trust and clarity.
- **Don't** treat secondary blue-grey or gold as competing accent colors. They support, they do not lead.
- **Don't** make surfaces float unless the layer change is real. Flat-by-default is mandatory.
