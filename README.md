# EchoBoard

EchoBoard is a local-first Windows desktop soundboard and audio-routing app. The product requirements live in [docs/PRD.md](docs/PRD.md).

This repository currently contains the development foundation only: solution structure, project boundaries, an empty WinUI shell, logging, settings, SQLite readiness, tests, and CI. Product features such as audio playback, microphone capture, mixing, hotkeys, sound import, Discord/OBS integration, and final UI are intentionally not implemented yet.

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

The app currently opens an empty shell window. Runtime logs are written under the user's local application data folder, not to the repository.

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
```

See [docs/architecture.md](docs/architecture.md) for project responsibilities and dependency rules.
