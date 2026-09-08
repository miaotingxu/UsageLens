# Terminal UI Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved D “极简终端” WPF interface on `feature/ui-terminal` while preserving all existing data and behavior.

**Architecture:** Recompose the expanded card into a dark monospaced terminal surface with two compact quota rows and three prompt-style Token rows. Only XAML, the known progress-track width, and README variant copy change; runtime services and interaction logic remain untouched.

**Tech Stack:** C# 14, .NET 10, WPF XAML, Cascadia Code/Consolas fallback, existing presentation and Token tests.

## Global Constraints

- Branch from `develop` after this plan commit.
- Keep logical window size `300×205`, card height `195`, 70% opacity, and collapsed handle `80×8`.
- Keep both quota values, both reset countdowns/timestamps, both progress tracks, all three Token totals/prices, and both update statuses visible.
- Preserve existing color bands, blue Token values, green prices, one-minute quota refresh, five-minute Token refresh, right-click menu, topmost state, and one-second auto-collapse.

---

### Task 1: Terminal layout

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: all existing named UI fields used by `MainWindow.xaml.cs`
- Produces: terminal header, two quota rows, three prompt-style Token rows, and unchanged status fields

- [ ] **Step 1: Create the terminal surface**

Use a near-black 70%-opaque panel, thin green border, monospaced type, restrained separators, and a compact `CODEX_QUOTA_FLOAT / ONLINE` header.

- [ ] **Step 2: Create quota command rows**

Each quota row shows label, colored percentage, `RESET` plus countdown/timestamp, and a 2-pixel progress track aligned under the reset text.

- [ ] **Step 3: Create prompt-style Token rows**

Each row begins with a green `>` marker and period label, followed by blue Token total and green estimated price. Add compact `Q` and `T` update times in the footer.

- [ ] **Step 4: Preserve dynamic progress sizing**

Set `QuotaProgressTrackWidth` to the exact terminal rail width declared in XAML so `UpdateQuotaProgress` cannot overflow.

- [ ] **Step 5: Build, test, and verify the live window**

Run the Release build and both test projects. Launch the built executable and assert all required labels are present, no page-switch buttons exist, and one-second collapse plus handle expansion succeeds.

- [ ] **Step 6: Commit**

```powershell
git add MainWindow.xaml MainWindow.xaml.cs README.md
git commit -m "feat: add terminal floating UI"
```
