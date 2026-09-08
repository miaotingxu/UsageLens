# Instrument UI Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved A “精密仪表” WPF interface on `feature/ui-instrument` without changing quota or Token data behavior.

**Architecture:** Replace only the expanded-card XAML and add a small pure geometry helper for circular quota arcs. `MainWindow` continues to own state and refreshes; it updates the two gauge paths instead of linear fills, while Token aggregation, pricing, timers, context menu, topmost behavior, and collapse controller stay unchanged.

**Tech Stack:** C# 14, .NET 10, WPF XAML, `System.Windows.Media.PathGeometry`, existing console-style presentation tests.

## Global Constraints

- Branch from the same `develop` baseline used by the B variant.
- Keep logical window size `300×205`, card height `195`, 70% opacity, and collapsed handle `80×8`.
- Keep all existing data and refresh behavior; no new settings, dependencies, database, or network calls.
- Token remains blue, price remains green, and quota gauge colors use the existing four-band rules.

---

### Task 1: Circular quota geometry

**Files:**
- Create: `Services/QuotaGaugeGeometry.cs`
- Modify: `tests/CodexQuotaFloat.PresentationTests/Program.cs`

**Interfaces:**
- Produces: `QuotaGaugeGeometry.CreateArc(int percent, double radius) -> Geometry`
- Consumes: clamped remaining percentage in the range `0..100`

- [ ] **Step 1: Add failing geometry assertions**

Assert that 0% returns `Geometry.Empty`, 50% produces an open semicircular figure, 100% produces a complete ring geometry, and out-of-range values clamp to `0..100`.

- [ ] **Step 2: Run the presentation test and confirm failure**

Run `dotnet run --project tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj -c Release`. Expected: compilation fails because `QuotaGaugeGeometry` does not exist.

- [ ] **Step 3: Implement the helper**

Use a `PathFigure` beginning at the 12 o’clock point and one or two `ArcSegment` values. Return `Geometry.Empty` for zero; use two 180-degree arcs for 100% so start and end coordinates do not coincide.

- [ ] **Step 4: Run the presentation test**

Expected: `QuotaPresentation tests passed.`

### Task 2: Instrument card layout

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: `QuotaGaugeGeometry.CreateArc`, existing `QuotaState`, `TokenUsageState`, and `GetQuotaBrush`
- Produces: named paths `FiveHourGaugeArc` and `WeeklyGaugeArc`, plus all existing named reset, Token, price, and status text elements

- [ ] **Step 1: Replace the quota area with two instrument gauges**

Create two side-by-side gauge cells. Each cell contains an `Ellipse` track, a named `Path` arc, percentage text centered in the ring, and the quota name plus countdown/timestamp to its right.

- [ ] **Step 2: Build the compact Token readout**

Add three rows with period at left, total Token in blue at center, and estimated price in green at right. Retain both update statuses in a dashed technical footer.

- [ ] **Step 3: Connect live values**

Replace calls to `UpdateQuotaProgress` with `UpdateQuotaGauge(Path gauge, int? remainingPercent)`. Set `gauge.Data` from `QuotaGaugeGeometry.CreateArc`, its stroke from `GetQuotaBrush`, and collapse it when quota data is absent.

- [ ] **Step 4: Build and run both test projects**

Run the Release build, Token tests, and presentation tests. Expected: zero warnings/errors and both test executables report success.

- [ ] **Step 5: Verify the live window**

Launch the branch build and verify one visible 300×205 logical WPF window contains both quota labels, both reset strings, all three Token periods, all prices, both status strings, and no page-switch buttons. Move the pointer away for 1.6 seconds and back to the 80×8 handle; verify collapse and expansion.

- [ ] **Step 6: Commit the branch**

```powershell
git add MainWindow.xaml MainWindow.xaml.cs Services/QuotaGaugeGeometry.cs tests/CodexQuotaFloat.PresentationTests/Program.cs README.md
git commit -m "feat: add precision instrument floating UI"
```

