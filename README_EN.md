<div align="center">

# UsageLens

### A lightweight, always-on-top Windows widget for Codex usage monitoring

[简体中文](README.md) · [English](README_EN.md)

[![Release](https://img.shields.io/github/v/release/miaotingxu/UsageLens?display_name=tag&sort=semver)](https://github.com/miaotingxu/UsageLens/releases/latest) [![License](https://img.shields.io/github/license/miaotingxu/UsageLens)](LICENSE) ![Windows](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4)

[Download latest release](https://github.com/miaotingxu/UsageLens/releases/latest) · [Quick start](#quick-start) · [简体中文](README.md)

Track Codex rate limits, reset countdowns, local token usage, and estimated costs.  
No extra login, no API key, and local session data stays on your computer.

</div>

## Why UsageLens

### Know your quota at a glance

See 5-hour and 7-day quota percentages, reset countdowns, exact reset times, and color-coded status.

### Understand local usage

Aggregate today, 7-day, and 30-day token usage from sessions kept on this computer, with model-based input and output cost estimates.

### Stay focused at the top of your screen

The widget stays attached to the primary screen's top edge and can only be dragged horizontally. It folds away after the pointer leaves and keeps a small 80×8 pixel handle. Four visual styles share the same data and interaction model.

## Interface styles

The current release includes Instrument, Glass, Timeline, and Terminal styles. Use the arrows on either side of the window to cycle through them; only the visual presentation changes.

![UsageLens four interface styles](assets/screenshots/ui-overview.png)

## Download and install

Download `UsageLens-v{version}-win-x64-portable.zip` from [Releases](https://github.com/miaotingxu/UsageLens/releases/latest).

This is a Windows x64 portable package. Extract it and run `UsageLens.exe`; no separate .NET Runtime installation is required.

> Codex must already be installed and signed in on this computer. UsageLens reuses Codex's existing local login state to read quota data.

## Requirements

| Item | Requirement |
| --- | --- |
| Operating system | Windows 10 or Windows 11, x64 |
| Codex | Installed and signed in |
| Network | Required when Codex fetches current quota data |
| .NET Runtime | Not required for the portable package |

## Quick start

1. Download the Portable ZIP from the latest Release.
2. Extract all files to a folder; do not run the executable from inside the archive.
3. Double-click `UsageLens.exe`.

## Controls

| Action | Result |
| --- | --- |
| Hold the left mouse button and move horizontally | Drag along the top edge of the primary screen |
| Click the left or right arrow | Cycle through the four UI styles |
| Move the pointer away for 1 second | Fold the card toward the top edge |
| Move the pointer onto the top handle | Expand the card |
| Right-click → Refresh | Refresh quota and token usage |
| Right-click → Exit | Close the application |

The selected style and horizontal position are stored in `%LOCALAPPDATA%\UsageLens\appearance.json`. It contains only the style and window X coordinate, not account, quota, or session data.
On first launch, if the new path has no valid settings, the app migrates the style and horizontal position from `%LOCALAPPDATA%\CodexQuotaFloat\appearance.json`; the legacy directory is never deleted.

## Data and privacy

Quota data is read through the local `codex app-server` using Codex's existing login state. It is read once at startup and refreshed every 60 seconds. Countdown text is updated locally.

Token usage is aggregated from session metadata in `%USERPROFILE%\.codex\sessions` for the most recent 30 local calendar days and refreshed every 5 minutes. Conversation bodies are not read, stored, or uploaded. Sessions from other computers, deleted records, and sessions that were never written locally are not included.

The application does not read or store `auth.json`, API keys, passwords, or conversation content.

Remaining quota colors are green for 81–100%, yellow for 50–80%, amber for 20–49%, red for 0–19%, and neutral gray when data is unavailable.

## Estimated token costs

Costs are estimates based on the model recorded in local session metadata and input/output tokens. Prices are per 1M tokens:

| Model | Input | Output |
| --- | ---: | ---: |
| GPT-5.6 Sol | $4.00 | $20.00 |
| GPT-5.6 Terra | $2.00 | $12.00 |
| GPT-5.6 Luna | $0.20 | $1.20 |
| GPT-6 Astra | $8.00 | $40.00 |
| codex-auto-review | $2.00 | $12.00 |

Tokens from unlisted models are excluded from the estimate. A trailing `*` indicates that some tokens could not be priced. These values are informational estimates, not actual invoices, plan value, or server-side quota deductions.

## Troubleshooting

### The window is not visible

Check the very top edge of the screen for the collapsed handle and move the pointer onto it. You can also close an existing `UsageLens.exe` process and start it again. Delete `%LOCALAPPDATA%\UsageLens\appearance.json` to reset style and position.

### Quota shows `--`

Make sure Codex is installed, signed in, and able to reach its service. After a successful read, temporary failures keep the last successful result while the app reconnects.

### Token usage shows zero

This means no readable Codex session metadata was found for the last 30 local calendar days; it is different from a read failure.

## Build from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/miaotingxu/UsageLens.git
cd UsageLens
dotnet build .\UsageLens.csproj -c Release
dotnet run --project .\UsageLens.csproj
```

Run tests:

```powershell
dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release
dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release
```

Build the portable package:

```powershell
.\scripts\package-release.ps1 -Version 1.3.1
```

## Roadmap

- Improve multi-monitor behavior.
- Add configurable refresh and folding behavior.
- Expand model and pricing coverage.
- Improve package signing and update experience.

## Contributing and license

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines. Report security issues privately using [SECURITY.md](SECURITY.md). Licensed under the [MIT License](LICENSE).
