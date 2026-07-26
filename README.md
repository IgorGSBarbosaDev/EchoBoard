# EchoBoard

EchoBoard is a local-first Windows desktop soundboard and audio-routing app. The product requirements live in [docs/PRD.md](docs/PRD.md).

The current application includes the local sound library, content-aware decoding, global hotkeys, centralized playback, continuous microphone capture, a 48 kHz float mixer, local effects monitoring, and routing to an external virtual audio endpoint.

## Prerequisites

- Windows 10/11, 64-bit
- .NET SDK `10.0.202` or a compatible .NET 10 SDK
- Windows App SDK / WinUI 3 tooling
- Visual Studio 2026 or newer with Windows desktop development tools, or equivalent CLI workloads when available

Check the installed SDK:

```powershell
dotnet --info
```

## Restore

```powershell
dotnet workload restore
dotnet restore EchoBoard.sln
```

## Build

```powershell
dotnet build EchoBoard.sln --configuration Release
```

## Test

```powershell
dotnet test EchoBoard.sln --configuration Release --no-build
```

## Run

```powershell
dotnet run --project src/EchoBoard.App/EchoBoard.App.csproj
```

Runtime logs and the SQLite library are written under the user's local application data folder, not to the repository.

## Virtual routing

EchoBoard does not install or implement an audio driver. To send microphone + effects to Discord or OBS, install an external cable such as VB-CABLE or VoiceMeeter, then select its render endpoint under **Settings → Mixer and routing → Virtual output**. The local monitor remains usable when no virtual cable is installed.

See [docs/audio-routing.md](docs/audio-routing.md) for the signal flow and validation checklist.

## Repository Layout

```text
src/
  EchoBoard.App/
  EchoBoard.Application/
  EchoBoard.Domain/
  EchoBoard.Audio/
  EchoBoard.Infrastructure/
tests/
  EchoBoard.Domain.Tests/
  EchoBoard.Application.Tests/
  EchoBoard.Audio.Tests/
  EchoBoard.Infrastructure.Tests/
docs/
  PRD.md
  architecture.md
  audio-routing.md
```

See [docs/architecture.md](docs/architecture.md) for project responsibilities and dependency rules.
