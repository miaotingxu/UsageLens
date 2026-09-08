# Timeline UI Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved C “横向时间轴” WPF interface on `feature/ui-timeline` while preserving all existing data and behavior.

**Architecture:** Recompose the existing named elements into two full-width quota timelines and a three-column Token statistics band. Only XAML, the known progress-track width, and README variant copy change; services, timers, parsing, pricing, context menu, and collapse controller stay unchanged.

**Tech Stack:** C# 14, .NET 10, WPF XAML, existing presentation and Token tests.

## Global Constraints

- Branch from `develop` after this plan commit.
- Keep logical window size `300×205`, card height `195`, 70% opacity, and collapsed handle `80×8`.
- Keep both quota values, both reset countdowns/timestamps, both progress tracks, all three Token totals/prices, and both update statuses visible.
- Preserve existing color bands, blue Token values, green prices, one-minute quota refresh, five-minute Token refresh, right-click menu, topmost state, and one-second auto-collapse.

---

### Task 1: Timeline layout

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: all existing named UI fields used by `MainWindow.xaml.cs`
- Produces: two horizontal quota rails, three Token columns, and unchanged status fields

- [ ] **Step 1: Create the technical header and quota rails**

Add a compact `CODEX QUOTA` header with a live indicator. Render each quota as label, full-width 2-pixel progress rail, reset annotation below the rail, and percentage aligned at the right edge.

- [ ] **Step 2: Create the Token statistics band**

Add a three-column band for `当日`, `近7天`, and `近30天`. Each column contains period, blue Token total, and green estimated price, separated by vertical hairlines.

- [ ] **Step 3: Preserve dynamic progress sizing**

Set `QuotaProgressTrackWidth` to the exact drawable timeline width declared in XAML so `UpdateQuotaProgress` cannot overflow.

- [ ] **Step 4: Build, test, and verify the live window**

Run the Release build and both test projects. Launch the built executable and assert all required labels are present, no page-switch buttons exist, and collapse/expansion moves between top `0` and the hidden card position.

- [ ] **Step 5: Commit**

```powershell
git add MainWindow.xaml MainWindow.xaml.cs README.md
git commit -m "feat: add timeline floating UI"
```

