# EchoBoard Design System

## Direction

EchoBoard uses a restrained audio-control-panel style: dark or light surfaces, electric blue actions, clear hierarchy, and subtle depth through borders and layered surfaces. Avoid decorative animation, heavy blur, complex gradients, and dashboard-style density that makes the app feel administrative.

## Theme Tokens

Theme resources live in `src/EchoBoard.App/Themes/` and are merged from `App.xaml`.

- `Colors.xaml`: theme-aware primitive colors for dark, light, and system-default fallback.
- `Palettes.xaml`: centralized dark/light accent palettes used by the global palette selector.
- `Brushes.xaml`: reusable brushes based on color tokens.
- `Typography.xaml`: shared font sizes for title, section title, body, caption, badge, and controls.
- `Spacing.xaml`: spacing scale and common padding values.
- `Radii.xaml`: corner radius values for controls, panels, cards, and badges.
- `ControlStyles.xaml`: global reusable styles for common WinUI controls.

Use `{ThemeResource ...}` for theme-aware color and brush references. Do not hardcode PRD palette hex values in pages or reusable controls.

The application starts in dark mode, persists the selected dark/light theme and accent palette in `AppSettings`, and restores both on launch. Runtime palette changes update the shared action, hover, pressed, focus, selection, and active-surface brushes so all screens remain consistent without duplicating colors in views.

## Typography

- Title: first-level screen or preview title.
- Section title: panel and card headings.
- Body: primary readable copy.
- Caption: secondary metadata, labels, or helper text.
- Badge: compact status labels.

Keep text concise and sized to its container. Do not use oversized hero text inside compact panels.

## Spacing And Radius

Use the `EchoBoardSpace*` scale for layout gaps and the shared padding resources for pages, panels, controls, icon buttons, and badges. Cards and panels use an 8px maximum radius by default; smaller controls use 6px or 4px. Pill radius is reserved for compact badges.

## Reusable Styles

Current reusable styles:

- `EchoBoardPrimaryButtonStyle`
- `EchoBoardSecondaryButtonStyle`
- `EchoBoardGhostButtonStyle`
- `EchoBoardDangerButtonStyle`
- `EchoBoardIconButtonStyle`
- `EchoBoardSearchTextBoxStyle`
- `EchoBoardToggleButtonStyle`
- `EchoBoardSliderStyle`
- `EchoBoardCardStyle`
- `EchoBoardPanelStyle`
- `EchoBoardStatusBadgeStyle`
- `EchoBoardSectionTitleTextStyle`
- `EchoBoardBodyTextStyle`
- `EchoBoardCaptionTextStyle`

Prefer these styles before adding custom controls. Add a custom control only when a repeated UI element needs behavior or structure that cannot be expressed cleanly with a style.

## Future Component Rules

- Keep views focused on layout and bind state/actions through view models.
- Add tokens before duplicating colors, spacing, or typography values.
- Keep audio-specific UI direct and scannable: levels, device state, transport state, and warnings should be visible without decorative noise.
- Use status color together with text or shape; do not rely on color alone.
- Validate both light and dark themes when adding a screen or reusable component.

## Dashboard And Sound Details

- The Dashboard uses four summary metrics and a proportional two-column content grid at 1100px and above, a 2x2 metric grid with stacked content from 720px to 1099px, and a single column below 720px.
- `SoundCard` keeps the card body as the playback action. Favorite and overflow-menu actions are separate controls and must never trigger playback through event bubbling.
- Waveforms render only persisted peaks extracted from the real audio file. Missing or unreadable waveform data uses a neutral unavailable state instead of simulated bars.
- `SoundDetailsDrawer` overlays the shell content from the right, is limited to 360px, and uses a short transition. Its closed state is `Collapsed`; no column, background, border, or invisible hit target may remain.
- The drawer must close by its close button, Escape, and backdrop, and focus returns to the control that opened it.
- Rounded rectangles remain subtle. Pill shapes are limited to hotkeys and compact status indicators.
