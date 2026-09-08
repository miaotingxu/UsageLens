# Floating Window Visual Prototypes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one local HTML comparison page containing four complete, interactive visual directions for the CodexQuotaFloat expanded and collapsed states.

**Architecture:** A single self-contained HTML file owns the sample data, all four card structures, shared design tokens, direction-specific CSS, and small vanilla-JavaScript focus/collapse controls. It remains isolated under `.superpowers/brainstorm/` so production WPF code and the release package are untouched.

**Tech Stack:** Semantic HTML5, CSS custom properties, CSS Grid/Flexbox, inline SVG, vanilla JavaScript, Chromium browser verification.

## Global Constraints

- Every direction displays 5-hour quota, 5-hour reset countdown and timestamp, progress, 7-day quota, 7-day reset countdown and timestamp, progress, today Token and price, 7-day Token and price, 30-day Token and price, quota update status, and Token update status.
- Use the same sample values in all four directions: `94%`, `76%`, `4h 55m · 09/09 03:29`, `6d 13h · 09/15 12:24`, `9703.85万`, `$151.10`, `5.84亿`, and `$478.66`.
- Preserve the semantic 300-pixel card width, 70% opaque dark surface, 80×8 collapsed handle, quota color bands, blue Token figures, and green price figures.
- Do not modify `MainWindow.xaml`, C# source files, tests, or `release-floating-auto`.

---

### Task 1: Four-direction comparison artifact

**Files:**
- Create: `.superpowers/brainstorm/floating-window-redesign-20260908/index.html`

**Interfaces:**
- Consumes: visual requirements in `docs/superpowers/specs/2026-09-08-floating-window-visual-directions-design.md`
- Produces: one self-contained page whose controls use `data-filter` and whose cards use `data-design` and `data-collapsed`

- [ ] **Step 1: Create semantic data-complete card markup**

Create a comparison header, filter buttons for `all`, `instrument`, `glass`, `timeline`, and `terminal`, and four `<article class="concept" data-design="…">` elements. Each article must contain two quota readings, two reset strings, two progress representations, three Token totals with prices, two updated timestamps, and one `.collapse-toggle` button.

- [ ] **Step 2: Implement four visually distinct systems**

Define shared sizing and color variables, then implement:

```css
.instrument-card { /* circular gauges + technical ticks */ }
.glass-card { /* layered glass quota tiles + token table */ }
.timeline-card { /* full-width quota rails + anchored annotations */ }
.terminal-card { /* monospaced dense rows + restrained separators */ }
```

Keep each `.float-card` at `width: 300px`; use `backdrop-filter`, gradients, borders, and shadows without external assets or libraries.

- [ ] **Step 3: Add comparison and collapse interactions**

Implement two bounded behaviors:

```js
document.querySelectorAll('[data-filter]').forEach((button) => {
  button.addEventListener('click', () => setFilter(button.dataset.filter));
});

document.querySelectorAll('.collapse-toggle').forEach((button) => {
  button.addEventListener('click', () => toggleCollapsed(button.closest('.concept')));
});
```

`setFilter('all')` displays all four concepts; another value displays only the matching concept. `toggleCollapsed` switches `data-collapsed` and reduces the visual to its 80×8 handle; clicking the handle restores it.

- [ ] **Step 4: Verify data completeness and responsive layout**

Open the file in Chromium and assert that every concept visibly contains these labels: `5小时`, `7天额度`, `当日`, `近7天`, `近30天`. Verify each concept contains two reset strings, three price strings, and both status timestamps; verify filter and collapse controls change visible state without console errors.

- [ ] **Step 5: Capture visual evidence and hand off**

Capture a full-page screenshot at a desktop viewport, keep the HTML tab open as the deliverable, and provide the local HTML and screenshot paths. Do not commit `.superpowers/brainstorm/` because it is a temporary design artifact.

### Task 2: Plan and artifact verification

**Files:**
- Verify: `.superpowers/brainstorm/floating-window-redesign-20260908/index.html`
- Verify unchanged: `MainWindow.xaml`

**Interfaces:**
- Consumes: completed Task 1 artifact
- Produces: evidence that the design-only work did not touch production files

- [ ] **Step 1: Check markup and required values**

Run focused text searches for the four `data-design` values, all required labels, reset timestamps, Token totals, prices, update times, `data-filter`, and `data-collapsed`. Expected: every required token is present and all four concepts are unique.

- [ ] **Step 2: Verify production isolation**

Run `git status --short` and `git diff -- MainWindow.xaml MainWindow.xaml.cs`. Expected: no production source changes; only this tracked plan may appear in Git because `.superpowers/brainstorm/` is ignored.

- [ ] **Step 3: Commit the implementation plan**

```powershell
git add docs/superpowers/plans/2026-09-08-floating-window-visual-prototypes.md
git commit -m "docs: plan floating window visual prototypes"
```
