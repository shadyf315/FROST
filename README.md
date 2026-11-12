# FROST ❄️

**A macOS-inspired desktop experience for Windows.**

FROST is an experimental Windows desktop companion focused on a calm, winter-inspired atmosphere and a more visual way to work with open windows.

## Planned features

- **Snow Engine** — configurable animated snowfall overlay.
- **Mission Control** — overview of open windows with quick switching.
- **Glass Engine** — translucent, macOS Sequoia-inspired surfaces where Windows allows.
- **Dock** — a customizable application launcher.
- **Settings** — central control for effects, behavior, and performance.

## Technology

- C#
- WPF
- .NET 10 for Windows
- Windows APIs (planned)

## Requirements

- Windows 10 or Windows 11
- .NET 10 SDK

## Build

```powershell
dotnet build .\FROST\FROST.csproj
dotnet run --project .\FROST\FROST.csproj
```

## Project status

Early development. The first commit establishes the WPF application shell and initial dashboard. System-wide effects and real window management are being developed in separate modules.

## License

MIT
