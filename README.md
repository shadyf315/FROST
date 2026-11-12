# FROST ❄️

**A macOS-inspired desktop experience for Windows.**

FROST is an experimental Windows desktop companion focused on a calm, winter-inspired atmosphere and a more visual way to work with open windows.

## Features

### Implemented
- **Desktop snowfall overlay** — transparent, click-through overlay spanning the virtual desktop.
- **Snow controls** — change flake density and fall speed while the overlay is running.
- **Persistent preferences** — snowfall density, speed, and enabled state are stored in the current Windows user profile.
- **Mission Control** — live list of visible application windows; select one to switch to it.
- **Global shortcuts** — `Ctrl + Alt + M` opens Mission Control; `Ctrl + Alt + N` toggles snowfall.
- **Dashboard** — dark, glass-inspired application shell with navigation and quick controls.

### Planned
- **Mission Control previews** — live window thumbnails and a spatial overview.
- **Glass Engine** — translucent, macOS Sequoia-inspired surfaces where Windows allows.
- **Dock** — a customizable application launcher.
- **Settings** — central control for effects, behavior, startup, and performance.

## Technology

- C#
- WPF
- .NET 10 for Windows
- Windows APIs

## Requirements

- Windows 10 or Windows 11
- .NET 10 SDK

## Build and run

Run these commands from the repository root:

```powershell
dotnet build .\FROST\FROST.csproj
dotnet run --project .\FROST\FROST.csproj
```

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + M` | Open Mission Control |
| `Ctrl + Alt + N` | Toggle snowfall |

## Notes

- The snowfall overlay is click-through and does not intentionally capture keyboard focus or mouse input.
- It covers the Windows virtual desktop, including multi-monitor layouts.
- Performance depends on display resolution and selected flake density.
- FROST is early-stage software. System-wide window styling is limited by Windows and the Desktop Window Manager; the project will use supported APIs rather than promise universal modification of third-party windows.

## Project status

Early development. Features are being added incrementally and will be validated on Windows.

## License

MIT
