# Glass UI Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved B “双层玻璃” WPF interface on `feature/ui-glass` without changing quota or Token data behavior.

**Architecture:** Recompose the existing named WPF elements into two translucent quota tiles above a darker three-row Token table. The presentation code keeps its existing linear progress update path with a track width adjusted for the smaller tile, while services, models, timers, pricing, menu, and collapse controller remain untouched.

**Tech Stack:** C# 14, .NET 10, WPF XAML, existing presentation and Token tests.

## Global Constraints

- Branch from the same `develop` baseline used by the A variant.
- Keep logical window size `300×205`, card height `195`, 70% opacity, and collapsed handle `80×8`.
- Keep all existing data and refresh behavior; no new settings, dependencies, database, or network calls.
- Token remains blue, price remains green, and quota percentages/progress use the existing four-band rules.

---

### Task 1: Layered glass card

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: existing named WPF fields used by `SetQuotaState`, `SetTokenUsageState`, and `UpdateQuotaProgress`
- Produces: two side-by-side quota tiles, one Token table, and two status fields with unchanged names

- [ ] **Step 1: Build the glass surface and quota tiles**

Use a translucent gradient card with a subtle highlight border. Place two equal-width rounded quota tiles in the first row; each tile contains its title, percentage, countdown/timestamp, and a 2-pixel progress track.

- [ ] **Step 2: Build the Token table**

Place a darker inset panel beneath the quota tiles. Add three 24-pixel rows with period at left, total Token in blue, and estimated price in green, separated by low-contrast 1-pixel lines.

- [ ] **Step 3: Preserve state and collapse bindings**

Keep every existing `x:Name` referenced by `MainWindow.xaml.cs`. Set `QuotaProgressTrackWidth` to the exact drawable width of one quota tile so fill widths never overflow. Retain `Card` height `195` and `HoverZone` top margin `195`.

- [ ] **Step 4: Update README variant description**

Describe the branch as the layered glass variant and state that quota and Token remain visible simultaneously.

- [ ] **Step 5: Build and run both test projects**

Run the Release build, Token tests, and presentation tests. Expected: zero warnings/errors and both test executables report success.

- [ ] **Step 6: Verify the live window**

Launch the branch build and verify one visible 300×205 logical WPF window contains both quota tiles, reset strings, progress tracks, all three Token rows and prices, both update statuses, and no page-switch buttons. Verify 1-second collapse and handle expansion.

- [ ] **Step 7: Commit the branch**

```powershell
git add MainWindow.xaml MainWindow.xaml.cs README.md
git commit -m "feat: add layered glass floating UI"
```
