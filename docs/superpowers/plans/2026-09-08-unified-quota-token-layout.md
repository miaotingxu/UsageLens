# Unified Quota and Token Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Present quota reset information and Token consumption together without page switching.

**Architecture:** Keep the existing refresh services and presentation state unchanged. Replace mutually-exclusive visual pages with a vertically stacked WPF card, then remove only page-navigation behavior.

**Tech Stack:** .NET 10, WPF, existing console presentation tests, Windows UI Automation.

## Global Constraints

- Keep width at 300px, 1-second auto-collapse, the 80×8 hover handle, transparency, Topmost behavior, and existing refresh cadence.
- Expanded card is 195px high and the window is 205px high.
- Do not change Token aggregation or model-pricing calculations.

---

### Task 1: Define the non-switching visual tree

**Files:**
- Modify: `MainWindow.xaml`
- Test: WPF build validation

**Interfaces:**
- Consumes: existing quota, reset, progress, Token and status named controls.
- Produces: a single always-visible quota region followed by a single always-visible Token region.

- [ ] **Step 1: Remove exclusive containers and page controls.**

Delete `QuotaPage`, `TokenPage`, `QuotaPageDot`, `TokenPageDot`, and the `PageDotButton` resource. Keep every display named element in the unified tree.

- [ ] **Step 2: Set expanded dimensions.**

Set Window Height to `205`, Card Height to `195`, and HoverZone top margin to `195`.

- [ ] **Step 3: Stack the regions.**

Use quota rows `42`, `46`, a 1px divider, three Token rows of `27`, and a shared status row. Retain the quota progress grids and Token text colors.

- [ ] **Step 4: Build.**

Run: `dotnet build .\CodexQuotaFloat.csproj -c Release`

Expected: no XAML named-element or event-handler errors.

### Task 2: Remove obsolete navigation behavior

**Files:**
- Modify: `MainWindow.xaml.cs`
- Test: `tests/CodexQuotaFloat.PresentationTests/Program.cs`

**Interfaces:**
- Consumes: `SetQuotaState` and `SetTokenUsageState`.
- Produces: state updates that target always-visible controls without page selection.

- [ ] **Step 1: Remove `QuotaPageButtonOnClick`, `TokenPageButtonOnClick`, and `ShowTokenPage`.**

- [ ] **Step 2: Preserve refresh timers, state models, and `SetTokenPeriod` unchanged.**

- [ ] **Step 3: Run logic tests.**

Run: `dotnet run --project .\tests\CodexQuotaFloat.PresentationTests\CodexQuotaFloat.PresentationTests.csproj -c Release`

Expected: `QuotaPresentation tests passed.`

- [ ] **Step 4: Run Token tests.**

Run: `dotnet run --project .\tests\CodexQuotaFloat.TokenUsageTests\CodexQuotaFloat.TokenUsageTests.csproj -c Release`

Expected: `Token usage tests passed.`

### Task 3: Validate and publish

**Files:**
- Modify: `README.md`
- Output: `release-floating-auto/CodexQuotaFloat.exe`

**Interfaces:**
- Consumes: built WPF application.
- Produces: one self-contained portable executable and updated interaction documentation.

- [ ] **Step 1: Replace page-dot instructions in README with same-card layout and 205px expanded height.**

- [ ] **Step 2: Publish into the existing `release-floating-auto` directory.**

Run: `dotnet publish .\CodexQuotaFloat.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\release-floating-auto`

- [ ] **Step 3: Launch and inspect the visible window with Windows UI Automation.**

Confirm it contains both quota labels and all three Token labels, and has no page-navigation Buttons.

- [ ] **Step 4: Commit.**

Run: `git add MainWindow.xaml MainWindow.xaml.cs README.md docs/superpowers/specs/2026-09-08-unified-quota-token-layout-design.md docs/superpowers/plans/2026-09-08-unified-quota-token-layout.md; git commit -m "feat: show quota and token usage together"`
