# Codex Quota Float Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a small Windows WPF overlay that shows the current locally logged-in Codex account's 5-hour and weekly quota remaining percentages.

**Architecture:** A single WPF application owns one topmost window and one long-lived direct child `codex app-server` process. Focused services parse JSON-RPC quota data and control the window state machine; `MainWindow` coordinates lifecycle, refresh and UI updates without an MVVM framework.

**Tech Stack:** C# 14, .NET 10, WPF, System.Text.Json, Windows 10/11.

## Global Constraints

- Create exactly one production WPF project named `CodexQuotaFloat`; do not add a database, backend, MVVM framework, UI library or NuGet dependency.
- Do not display a login page or read/store credentials, API keys, tokens or `auth.json`.
- Start `codex app-server --listen stdio://` directly via `ProcessStartInfo`, never through `cmd.exe` or PowerShell.
- Prefer `rateLimitsByLimitId["codex"]`, then `rateLimits`; identify 5-hour and weekly windows only by 300 and 10080 `windowDurationMins`.
- Use a 60-second refresh timer, preserve prior successful values when a later read fails, and retry a dead server at 3, 10, then 30-second intervals.
- Render a 360 x 140 px primary-monitor-top-center window with two-line quota details, reset countdowns, colored percentages and a concise status line; collapse by the card's actual height, leaving an 8 px high, 110 px wide bottom-center hover zone.
- Do not add tray support, settings, startup registration, position persistence, multi-display support, history, charts, alerts, syncing or auto-update.

---

## File structure

- `CodexQuotaFloat.csproj`: Windows/WPF project targeting `net10.0-windows`.
- `App.xaml`, `App.xaml.cs`: app startup/shutdown and named `Mutex` single-instance guard.
- `MainWindow.xaml`, `MainWindow.xaml.cs`: visual tree, context menu, refresh timer and service wiring.
- `Models/QuotaState.cs`: immutable display state and freshness enum.
- `Services/QuotaParser.cs`: pure response parser; no process or WPF dependencies.
- `Services/CodexAppServerClient.cs`: direct process lifecycle, JSONL reader, request correlation and reconnect support.
- `Services/FloatingWindowController.cs`: UI-thread state machine and slide animation.
- `README.md`: prerequisites, build/run instructions and troubleshooting.

### Task 1: Project shell and fixed visual contract

**Files:**
- Create: `CodexQuotaFloat.csproj`, `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `README.md`, `.gitignore`

**Interfaces:**
- Produces `MainWindow.SetQuotaState(QuotaState state)` for later services.
- Produces `App` shutdown hook for later App Server cleanup.

- [ ] **Step 1: Create the WPF project with no external dependencies.**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create the borderless 360 x 140 topmost window, two-line quota details, reset countdowns, colored percentages and concise status line.**

```xml
<Window Width="360" Height="140" WindowStyle="None" AllowsTransparency="True"
        Background="Transparent" Topmost="True" ShowInTaskbar="False">
  <!-- two name/countdown/value rows, a status line, and a bottom-center 110 x 8 hover zone -->
</Window>
```

- [ ] **Step 3: Center it on the primary working area at `Top = 0` after the window loads.**

```csharp
Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
Top = 0;
```

- [ ] **Step 4: Build the project.**

Run: `dotnet build .\CodexQuotaFloat.csproj -c Release`

Expected: `Build succeeded` with no warnings.

### Task 2: Data state and duration-driven parser

**Files:**
- Create: `Models/QuotaState.cs`, `Services/QuotaParser.cs`
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Produces `QuotaState QuotaParser.Parse(JsonElement result, DateTimeOffset now)`.
- Consumes raw JSON-RPC `result`, with `rateLimitsByLimitId` and fallback `rateLimits`.
- Produces `QuotaState` with `int? FiveHourRemaining`, `int? WeeklyRemaining`, reset times, timestamp and freshness status.

- [ ] **Step 1: Define an immutable quota state and the four freshness states.**

```csharp
public enum QuotaStatus { Loading, Ready, Stale, Offline }
public sealed record QuotaState(
    int? FiveHourRemaining, int? WeeklyRemaining,
    DateTimeOffset? FiveHourResetAt, DateTimeOffset? WeeklyResetAt,
    DateTimeOffset LastUpdatedAt, QuotaStatus Status);
```

- [ ] **Step 2: Implement parser selection and window collection.**

```csharp
var snapshot = SelectSnapshot(result); // rateLimitsByLimitId.codex, then rateLimits
foreach (var window in new[] { snapshot.primary, snapshot.secondary })
{
    // match windowDurationMins: 300 or 10080
}
```

- [ ] **Step 3: Guard malformed fields and clamp percentage values.**

```csharp
var used = Math.Clamp(window.GetProperty("usedPercent").GetInt32(), 0, 100);
var remaining = 100 - used;
```

- [ ] **Step 4: Wire `SetQuotaState` so null values render `--` and loading values render `...`.**

- [ ] **Step 5: Build again.**

Run: `dotnet build .\CodexQuotaFloat.csproj -c Release`

Expected: `Build succeeded` with no warnings.

### Task 3: Floating-window controller

**Files:**
- Create: `Services/FloatingWindowController.cs`
- Modify: `MainWindow.xaml`, `MainWindow.xaml.cs`

**Interfaces:**
- Produces `Expand()`, `ScheduleCollapse()`, `CancelCollapse()`, `Dispose()`.
- Consumes the owner window, its mouse events and dispatcher.
- Guarantees exactly one 3-second collapse timer and a 180 ms position animation.

- [ ] **Step 1: Define the state enum and controller constants.**

```csharp
private const double ExpandedTop = 0;
private double CollapsedTop => -(_card.ActualHeight > 0 ? _card.ActualHeight : _card.Height);
private static readonly TimeSpan CollapseDelay = TimeSpan.FromSeconds(3);
```

- [ ] **Step 2: Bind root `MouseEnter` to cancellation and `MouseLeave` to delayed collapse; bind hover-zone entry to immediate expansion.**

- [ ] **Step 3: Animate `Window.Top` with a `DoubleAnimation` for 180 ms and update the state at completion.**

- [ ] **Step 4: Verify manually: leave for less than three seconds, leave for more than three seconds, then enter the remaining six-pixel area.**

Expected: the first action stays open; the second collapses; the third immediately expands.

### Task 4: Direct App Server JSON-RPC client

**Files:**
- Create: `Services/CodexAppServerClient.cs`

**Interfaces:**
- Produces `Task StartAsync(CancellationToken)`, `Task<JsonElement> ReadRateLimitsAsync(CancellationToken)`, `Task StopAsync()` and `event EventHandler? Exited`.
- Uses `TaskCompletionSource<JsonElement>` keyed by numeric JSON-RPC request ID.
- Sends `initialize`, `initialized`, then `account/rateLimits/read` over newline-delimited JSON.

- [ ] **Step 1: Resolve Codex with `where.exe codex` and select an `.exe` result.**

```csharp
using var where = Process.Start(new ProcessStartInfo("where.exe", "codex") {
    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
});
```

- [ ] **Step 2: Start the executable directly and drain standard error separately.**

```csharp
new ProcessStartInfo(executable, "app-server --listen stdio://") {
    UseShellExecute = false, RedirectStandardInput = true,
    RedirectStandardOutput = true, RedirectStandardError = true,
    CreateNoWindow = true
};
```

- [ ] **Step 3: Run a single background output-reader loop and correlate JSON response IDs.**

```csharp
while (await output.ReadLineAsync(cancellationToken) is { } line)
{
    using var message = JsonDocument.Parse(line);
    CompleteMatchingRequest(message.RootElement);
}
```

- [ ] **Step 4: Complete initialization before allowing reads and fail outstanding requests if the process exits.**

- [ ] **Step 5: Build the project.**

Run: `dotnet build .\CodexQuotaFloat.csproj -c Release`

Expected: `Build succeeded` with no warnings.

### Task 5: Lifecycle, refresh, recovery and exit

**Files:**
- Modify: `App.xaml.cs`, `MainWindow.xaml.cs`, `Services/CodexAppServerClient.cs`, `README.md`

**Interfaces:**
- `MainWindow` owns one client, a 60-second `DispatcherTimer`, and a cancellable background refresh gate.
- `App` owns only the process-wide `Mutex` and coordinated disposal.

- [ ] **Step 1: Fetch at startup and every 60 seconds without blocking the UI thread.**

```csharp
_refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
_refreshTimer.Tick += async (_, _) => await RefreshAsync();
```

- [ ] **Step 2: Preserve the latest successful state if a later refresh fails, marking it `Stale`; show `--` only before a first success.**

- [ ] **Step 3: Recreate a dead App Server after delays 3, 10 and 30 seconds, then every 30 seconds; cancel retries at shutdown.**

- [ ] **Step 4: Add a context menu with only `刷新` and `退出`; make refresh non-blocking and exit stop the timer, close stdin, cancel readers and kill the child process if needed.**

- [ ] **Step 5: Add named-mutex single-instance protection and document prerequisites.**

```csharp
new Mutex(true, "Local\\CodexQuotaFloat", out var createdNew);
if (!createdNew) { Shutdown(); return; }
```

- [ ] **Step 6: Verify build and behavior.**

Run: `dotnet build .\CodexQuotaFloat.csproj -c Release`

Expected: `Build succeeded` with no warnings.

Manual checklist: launch once; launch a second copy; verify two quota rows; select `刷新`; wait 60 seconds; leave/re-enter the window; select `退出`; confirm no `codex app-server` child remains.

## Plan self-review

- Spec coverage: Tasks 1–5 cover the exact window shape, state machine, direct App Server session, duration-based parsing, refresh, error preservation, retry, single instance, context-menu exit and exclusions.
- Placeholder scan: no TBD/TODO or implicit error-handling steps remain.
- Type consistency: `QuotaState`, `QuotaParser.Parse`, `CodexAppServerClient.ReadRateLimitsAsync`, and `FloatingWindowController` are introduced before their consumers.
- Repository status: `D:\Code\demo` is not a Git repository. Do not initialize or commit a repository unless the user asks.
