# EchoBoard Architecture

This document describes the foundation-level architecture. Product behavior, MVP scope, and design direction remain in [PRD.md](PRD.md).

## Project Responsibilities

- `EchoBoard.App`: WinUI 3 UI, views, view models, controls, themes, navigation, and composition root.
- `EchoBoard.Application`: use cases, DTOs, interfaces, validation, and application services.
- `EchoBoard.Domain`: entities, value objects, business rules, enums, and domain exceptions.
- `EchoBoard.Audio`: capture, playback, device discovery, mixing, rendering, and waveform processing.
- `EchoBoard.Infrastructure`: SQLite, files, settings, hotkeys, logging, and Windows integrations.

## Dependency Rules

```text
EchoBoard.App -> EchoBoard.Application, EchoBoard.Audio, EchoBoard.Infrastructure
EchoBoard.Application -> EchoBoard.Domain
EchoBoard.Audio -> EchoBoard.Application, EchoBoard.Domain
EchoBoard.Infrastructure -> EchoBoard.Application, EchoBoard.Domain
EchoBoard.Domain -> no project dependencies
```

Circular dependencies are not allowed. The architecture tests read project files directly and fail if the approved reference graph changes.

## Boundary Rules

- View models must not access SQLite, file system APIs, NAudio, or Win32 APIs directly.
- Business rules belong in `EchoBoard.Domain`, not XAML code-behind or infrastructure.
- Real-time audio code belongs in `EchoBoard.Audio` and must not access UI, SQLite, files, or network resources from audio callbacks.
- Persistence and Windows integrations belong in `EchoBoard.Infrastructure`.
- The app remains local-first. Do not add backend services, accounts, cloud sync, telemetry, or a custom audio driver unless the PRD is explicitly changed first.

## Current Foundation

- WinUI startup uses `Microsoft.Extensions.Hosting` for dependency injection.
- `EchoBoard.App` registers the application, audio, and infrastructure modules from the composition root.
- Serilog writes rolling file logs under local application data.
- SQLite is active through EF Core with `Sound`, `Category`, `HotkeyBinding`, `AppSetting`, and `RecentlyPlayed` persistence, repository implementations, migrations, and a design-time factory. Playback history is removed in cascade with its sound.
- The library application layer contains import, listing/query, category management, favorite toggling, sound category assignment, persisted waveform extraction, play counts, and recent-history use cases.
- `PlaySoundUseCase` is the single entry point for card, drawer, and hotkey playback. It applies loop, stop-previous, and overlap policies before calling the audio engine and records history only after playback starts successfully.
- `AudioFileDecoder` detects the container and codec from file content before using the extension as a fallback. Import metadata, waveform generation, and playback share this decoder, including Ogg/Opus files with a misleading extension.
- `WasapiSoundPlaybackEngine` is registered once and exposed as both `ISoundPlaybackEngine` and `IAudioRoutingEngine`. It maintains a 48 kHz, stereo, float mixer for the application lifetime instead of opening one WASAPI session per sound.
- The virtual bus receives continuous microphone PCM plus effect sources. The local monitor bus receives effects only. Each bus has its own render endpoint and gain/limiter stage so one unavailable route does not stop the other.
- WASAPI render sessions are created through an audio-layer factory. Monitor and virtual routes negotiate their endpoint formats, reconnect independently by MMDevice ID, and can be exercised with hardware-free fakes in tests.
- Known virtual-device families are classified during endpoint enumeration. Settings highlights likely cables, preserves disconnected selections, and rejects same-family input/output combinations that would create feedback.
- The active audio profile is persisted as versioned `AppSetting` keys: microphone, monitor, virtual output, independent volumes, independent mutes, and monitor enabled state. `AudioRoutingSettingsCoordinator` exposes that single observable profile to Settings and the global player while applying every change to the same mixer instance. No audio-profile schema table is required.
- `PlaybackCoordinator` owns the shared UI command, timeline, seek operation, and observable snapshot used by Dashboard, Library, Favorites, Recent, the details drawer, and the global player. The shell is the only playback timer owner. Normal playback has no success banner; failures use the compact shell notification.
- The main shell hosts Dashboard, Library, Favorites, Recent, Settings, and Audio Diagnostics routes. A shared `SoundDetailsViewModel` keeps the selected sound and controls an overlay drawer used by Dashboard, Library, Favorites, and Recent without reserving layout width while closed.
- The Dashboard, Settings, player, and Audio Diagnostics derive microphone and routing state from the same engine snapshot.
