# AGENTS.md

## Project

EchoBoard is a Windows desktop soundboard and audio-routing app. It imports local audio, plays sounds through clicks or global keyboard hotkeys, mixes them with a physical microphone, and sends the result to a virtual audio device for Discord and OBS.

Local-first only. Do not add a backend, accounts, cloud services, telemetry, or a custom audio driver unless explicitly requested.

## Stack

- C# / .NET 10
- WinUI 3 + XAML
- MVVM with CommunityToolkit.Mvvm
- NAudio + WASAPI
- SQLite + Entity Framework Core
- Serilog
- xUnit + FluentAssertions
- GitHub Actions
- Windows 10/11, 64-bit only

## Architecture

- `EchoBoard.App`: UI, views, view models, controls, themes, navigation.
- `EchoBoard.Application`: use cases, DTOs, interfaces, validation.
- `EchoBoard.Domain`: entities, value objects, business rules, enums, exceptions.
- `EchoBoard.Audio`: capture, playback, devices, mixing, rendering, waveform processing.
- `EchoBoard.Infrastructure`: SQLite, files, settings, hotkeys, logging, Windows integrations.

Dependency rules:

```text
App -> Application -> Domain
Audio -> Application + Domain
Infrastructure -> Application + Domain
```

`Domain` must not depend on other projects. Avoid circular dependencies.

## Implementation Rules

- Prefer the simplest solution that fully meets the current requirement.
- Avoid overengineering, speculative abstractions, unused folders, placeholder code, and unnecessary packages.
- Keep changes focused. Do not refactor unrelated code.
- Use clear names, small cohesive classes, short focused methods, guard clauses, and composition over inheritance.
- Keep business logic out of code-behind, views, and infrastructure.
- Keep views focused on layout; use MVVM bindings and commands.
- Do not access SQLite, file system, NAudio, or Win32 APIs directly from view models.
- Use `async`/`await` for I/O, propagate `CancellationToken`, and never use `.Result` or `.Wait()`.
- Do not use `Task.Run` to hide blocking work; fix the blocking operation.
- Enable nullable reference types and address warnings instead of suppressing them.

## Audio Rules

- Audio stability and low latency are critical.
- Keep real-time audio code inside `EchoBoard.Audio`.
- Use WASAPI through NAudio.
- Do not access UI, SQLite, files, or network resources from audio callbacks.
- Avoid blocking, disk I/O, unnecessary allocations, and locks in audio paths.
- Use a consistent internal format where practical: PCM float, 48 kHz.
- Keep microphone, effects, monitor, and virtual-output volumes independent.
- Handle unavailable or disconnected devices without crashing.
- Do not implement a custom virtual-audio driver unless explicitly requested.

## Persistence, Testing, and Delivery

- Use EF Core migrations for every SQLite schema change.
- Do not commit local databases, logs, user settings, binaries, or personal audio files.
- Add or update tests for changed business rules and application behavior.
- Do not require real audio devices in unit tests; use mocks or fakes.
- Keep manual test checklists for Discord, OBS, virtual routing, and device disconnects.
- Update `docs/PRD.md` when product scope changes.
- Before finishing: build the solution, run relevant tests, update docs when needed, and report changed files, tests run, and known limitations.
