# EchoBoard Design System

## Direction

EchoBoard uses a fixed dark audio-control-panel style. Black and graphite surfaces, neutral actions, and fine borders create depth. Grayscale status colors, labels, and icons keep audio states clear. Use tonal surface layers instead of decorative gradients, heavy blur, or dashboard-style density.

## Theme Tokens

Theme resources live in `src/EchoBoard.App/Themes/` and are merged from `App.xaml`.

- `Colors.xaml`: primitive colors for the dark theme and its system-default fallback.
- `Brushes.xaml`: reusable brushes based on color tokens.
- `Typography.xaml`: shared font sizes for title, section title, body, caption, badge, and controls.
- `Spacing.xaml`: spacing scale and common padding values.
- `Radii.xaml`: corner radius values for controls, panels, cards, and badges.
- `ControlStyles.xaml`: global reusable styles for common WinUI controls.

Use `{ThemeResource ...}` for theme-aware color and brush references. Do not hardcode PRD palette hex values in pages or reusable controls.

The application always requests the dark theme. Theme and accent-palette preferences are not offered or persisted. Shared action, hover, pressed, focus, selection, and active-surface brushes keep screens consistent without duplicating colors in views.

## Typography

- Title: first-level screen or preview title.
- Section title: panel and card headings.
- Body: primary readable copy.
- Caption: secondary metadata, labels, or helper text.
- Badge: compact status labels.

Keep text concise and sized to its container. Do not use oversized hero text inside compact panels.

## Spacing And Radius

Use the `EchoBoardSpace*` scale for layout gaps and the shared padding resources for pages, panels, controls, icon buttons, badges, and device status cards. Cards and panels use an 8px maximum radius by default; smaller controls use 6px or 4px. Pill radius is reserved for compact badges, never for device status cards.

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
- `EchoBoardDeviceStatusCardStyle`
- `EchoBoardSectionTitleTextStyle`
- `EchoBoardBodyTextStyle`
- `EchoBoardCaptionTextStyle`

Prefer these styles before adding custom controls. Add a custom control only when a repeated UI element needs behavior or structure that cannot be expressed cleanly with a style.

## Future Component Rules

- Keep views focused on layout and bind state/actions through view models.
- Add tokens before duplicating colors, spacing, or typography values.
- Keep audio-specific UI direct and scannable: levels, device state, transport state, and warnings should be visible without decorative noise.
- Keep status identifiable by text and icon in the dark theme, using grayscale colors.
- Validate the dark theme when adding a screen or reusable component.

## Library, Settings, And Sound Details

- The shell exposes Library and Settings. Favorites remain a filter in Library; recent playback stays in internal history without a dedicated page. Device configuration, routing, and audio diagnostics are combined in the single Settings page.
- `SoundCard` keeps the card body as the playback action. Favorite and overflow-menu actions are separate controls and must never trigger playback through event bubbling.
- Waveforms render only persisted peaks extracted from the real audio file. Missing or unreadable waveform data uses a neutral unavailable state instead of simulated bars.
- `SoundDetailsDrawer` overlays the shell content from the right, is limited to 360px, and uses a short transition. Its closed state is `Collapsed`; no column, background, border, or invisible hit target may remain.
- The drawer must close by its close button, Escape, and backdrop, and focus returns to the control that opened it.
- Rounded rectangles remain subtle. Pill shapes are limited to hotkeys and compact status indicators.
