# Reset Time Font Size Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将四套悬浮窗 UI 的两组额度重置时间统一放大到 9 号字体。

**Architecture:** 只修改现有 XAML 中命名为 `FiveHourResetValue` 和 `WeeklyResetValue` 的八个 `TextBlock`。不修改布局、窗口尺寸、数据绑定或交互代码。

**Tech Stack:** WPF XAML、C#、.NET 10。

## Global Constraints

- 四套 UI 的重置时间统一使用 `FontSize="9"`。
- Terminal 的 `RESET` 标签继续使用 `FontSize="7"`。
- 不修改其他字号、行高、间距或窗口尺寸。
- 继续覆盖 `release-floating-auto`，不创建新发布目录。

---

### Task 1: 统一四套 UI 的重置时间字号

**Files:**
- Modify: `Views/InstrumentDashboard.xaml`
- Modify: `Views/GlassDashboard.xaml`
- Modify: `Views/TimelineDashboard.xaml`
- Modify: `Views/TerminalDashboard.xaml`

**Interfaces:**
- Consumes: `MainWindow.SetStyleText("FiveHourResetValue", value)` 和 `MainWindow.SetStyleText("WeeklyResetValue", value)`
- Produces: 四套 UI 中统一为 9 号字体的重置时间文本。

- [ ] **Step 1: 确认修改前字号分布**

Run: `rg -n "FiveHourResetValue|WeeklyResetValue" Views\\*Dashboard.xaml`

Expected: Terminal 为 7，其余三套为 8。

- [ ] **Step 2: 修改八个重置时间字段**

将下列两个命名元素在四个 Dashboard 中的 `FontSize` 统一设为 `9`：

```xml
<TextBlock x:Name="FiveHourResetValue" FontSize="9" ... />
<TextBlock x:Name="WeeklyResetValue" FontSize="9" ... />
```

Terminal 中无 `x:Name` 的 `RESET` 标签仍保留：

```xml
<TextBlock FontSize="7" Text="RESET  " ... />
```

- [ ] **Step 3: 验证八个目标字段和两个 Terminal 标签**

Run: `rg -n "FiveHourResetValue|WeeklyResetValue|Text=\"RESET  \"" Views\\InstrumentDashboard.xaml Views\\GlassDashboard.xaml Views\\TimelineDashboard.xaml Views\\TerminalDashboard.xaml`

Expected: 八个命名字段均为 9，两个 Terminal `RESET` 标签仍为 7。

- [ ] **Step 4: 构建并运行现有自检**

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
git commit -m "style: enlarge quota reset time text"
```
