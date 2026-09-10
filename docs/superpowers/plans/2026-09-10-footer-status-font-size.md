# Footer Status Font Size Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将四套悬浮窗 UI 的底部额度状态和 Token 状态统一为 9 号字体。

**Architecture:** 只修改四个 Dashboard XAML 中的底部状态 `TextBlock`。Terminal 的 `Q`、`T` 前缀同步调整，其他布局和逻辑保持不变。

**Tech Stack:** WPF XAML、C#、.NET 10。

## Global Constraints

- `StatusValue` 和 `TokenStatusValue` 统一使用 `FontSize="9"`。
- Terminal 的 `Q`、`T` 前缀同步使用 `FontSize="9"`。
- 不修改底栏高度、列宽、边距、颜色、字体系列或文本截断方式。
- 继续覆盖 `release-floating-auto`，不创建新发布目录。

---

### Task 1: 统一底部状态字号

**Files:**
- Modify: `Views/InstrumentDashboard.xaml`
- Modify: `Views/GlassDashboard.xaml`
- Modify: `Views/TimelineDashboard.xaml`
- Modify: `Views/TerminalDashboard.xaml`

**Interfaces:**
- Consumes: `MainWindow.SetStatusText(...)` 和 `MainWindow.SetTokenStatus(...)`
- Produces: 四套 UI 中统一为 9 号字体的底部状态区域。

- [ ] **Step 1: 确认修改前字号分布**

Run: `rg -n "StatusValue|TokenStatusValue|Text=\"Q \"|Text=\"T \"" Views\\*Dashboard.xaml`

Expected: Glass、Timeline 为 9；Instrument、Terminal 为 8。

- [ ] **Step 2: 修改目标字段**

四套 UI 中命名状态字段统一为：

```xml
<TextBlock x:Name="StatusValue" FontSize="9" ... />
<TextBlock x:Name="TokenStatusValue" FontSize="9" ... />
```

Terminal 的前缀统一为：

```xml
<TextBlock FontSize="9" Text="Q " ... />
<TextBlock FontSize="9" Text="T " ... />
```

- [ ] **Step 3: 验证字段覆盖**

Run: `rg -n "StatusValue|TokenStatusValue|Text=\"Q \"|Text=\"T \"" Views\\InstrumentDashboard.xaml Views\\GlassDashboard.xaml Views\\TimelineDashboard.xaml Views\\TerminalDashboard.xaml`

Expected: 八个命名状态字段和两个 Terminal 前缀均为 9。

- [ ] **Step 4: 构建并运行全部自检**

Run: `dotnet build CodexQuotaFloat.csproj --configuration Release --no-restore`

Expected: 0 warnings, 0 errors.

Run: `dotnet run --project tests\\CodexQuotaFloat.PresentationTests\\CodexQuotaFloat.PresentationTests.csproj --configuration Release --no-restore`

Expected: 输出 `QuotaPresentation tests passed.`。

Run: `dotnet run --project tests\\CodexQuotaFloat.TokenUsageTests\\CodexQuotaFloat.TokenUsageTests.csproj --configuration Release --no-restore`

Expected: 输出 `Token usage tests passed.`。

- [ ] **Step 5: 覆盖便携发布目录并提交**

Run: `dotnet publish CodexQuotaFloat.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output release-floating-auto`

```bash
git add Views/InstrumentDashboard.xaml Views/GlassDashboard.xaml Views/TimelineDashboard.xaml Views/TerminalDashboard.xaml
git commit -m "style: unify footer status text size"
```
