# Top-Anchored Horizontal Drag Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让悬浮窗始终吸附主屏工作区顶部，并支持流畅、受边界约束的纯水平拖拽。

**Architecture:** 使用独立的 `HorizontalWindowPlacement` 计算初始位置和水平边界，便于无 UI 单元测试。`MainWindow` 通过 WPF 鼠标捕获处理拖拽，`FloatingWindowController` 只维护固定的顶部展开基准和折叠状态；外观配置仅持久化样式与横坐标。

**Tech Stack:** C# 13、.NET 10、WPF、System.Text.Json、现有控制台自检项目。

## Global Constraints

- 窗口只能在主屏幕工作区顶部左右移动。
- 展开卡片和 80x8 折叠把手都可拖动。
- 拖动期间暂停自动折叠，离开后仍按 1 秒延迟折叠。
- 四套 UI、右键菜单、置顶、刷新、透明度和数据逻辑不变。
- 继续覆盖 `release-floating-auto`，不创建新发布目录。

---

### Task 1: 水平位置计算与配置迁移

**Files:**
- Create: `Services/HorizontalWindowPlacement.cs`
- Modify: `Services/AppearanceSettingsStore.cs`
- Modify: `tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj`
- Modify: `tests/CodexQuotaFloat.PresentationTests/Program.cs`

**Interfaces:**
- Produces: `HorizontalWindowPlacement.ClampLeft(double requestedLeft, double workAreaLeft, double workAreaWidth, double windowWidth) -> double`
- Produces: `HorizontalWindowPlacement.ResolveInitialLeft(double? savedLeft, double workAreaLeft, double workAreaWidth, double windowWidth) -> double`
- Produces: `FloatingWindowAppearance(FloatingStyleKind Style, double? Left)`
- Produces: `AppearanceSettingsStore.Save(FloatingStyleKind style, double left)`

- [ ] **Step 1: 为居中、左边界、右边界和有效保存位置编写失败断言**

```csharp
AssertNear(824, HorizontalWindowPlacement.ResolveInitialLeft(null, 0, 2000, 352));
AssertNear(0, HorizontalWindowPlacement.ClampLeft(-50, 0, 2000, 352));
AssertNear(1648, HorizontalWindowPlacement.ClampLeft(1900, 0, 2000, 352));
AssertNear(640, HorizontalWindowPlacement.ResolveInitialLeft(640, 0, 2000, 352));
```

- [ ] **Step 2: 运行展示自检并确认因类型不存在而失败**

Run: `dotnet run --project tests\\CodexQuotaFloat.PresentationTests\\CodexQuotaFloat.PresentationTests.csproj --configuration Release --no-restore`

Expected: FAIL，提示 `HorizontalWindowPlacement` 未定义。

- [ ] **Step 3: 实现纯水平位置计算**

```csharp
public static class HorizontalWindowPlacement
{
    public static double ResolveInitialLeft(double? savedLeft, double workAreaLeft, double workAreaWidth, double windowWidth) =>
        ClampLeft(savedLeft is double value && double.IsFinite(value)
            ? value
            : workAreaLeft + (workAreaWidth - windowWidth) / 2,
            workAreaLeft,
            workAreaWidth,
            windowWidth);

    public static double ClampLeft(double requestedLeft, double workAreaLeft, double workAreaWidth, double windowWidth)
    {
        var maximumLeft = Math.Max(workAreaLeft, workAreaLeft + workAreaWidth - windowWidth);
        return Math.Clamp(requestedLeft, workAreaLeft, maximumLeft);
    }
}
```

- [ ] **Step 4: 将外观配置改为只返回并保存 `Style` 与 `Left`**

读取 DTO 保留可选 `Top` 字段以兼容现有 JSON，但返回值忽略它；写入 DTO 只包含 `Style` 和有限的 `Left`，使下一次保存自动移除旧 `Top`。

- [ ] **Step 5: 运行展示自检并确认通过**

Run: `dotnet run --project tests\\CodexQuotaFloat.PresentationTests\\CodexQuotaFloat.PresentationTests.csproj --configuration Release --no-restore`

Expected: 输出 `QuotaPresentation tests passed.`。

### Task 2: WPF 纯水平拖拽与顶部折叠锚点

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `Services/FloatingWindowController.cs`

**Interfaces:**
- Consumes: `HorizontalWindowPlacement.ClampLeft(...)`
- Consumes: `HorizontalWindowPlacement.ResolveInitialLeft(...)`
- Produces: `FloatingWindowController.BeginUserDrag(double expandedTop)`
- Produces: `FloatingWindowController.CompleteUserDrag(double expandedLeft, double expandedTop)`

- [ ] **Step 1: 在窗口注册移动、左键释放和鼠标捕获丢失事件**

```xml
PreviewMouseMove="WindowOnPreviewMouseMove"
PreviewMouseLeftButtonUp="WindowOnPreviewMouseLeftButtonUp"
LostMouseCapture="WindowOnLostMouseCapture"
```

卡片与折叠把手继续使用 `DragSurfaceOnPreviewMouseLeftButtonDown` 作为拖拽起点。

- [ ] **Step 2: 用屏幕坐标记录拖拽起点并捕获鼠标**

按下左键时保存鼠标屏幕横坐标与窗口 `Left`，调用 `BeginUserDrag(SystemParameters.WorkArea.Top)`，再调用 `Mouse.Capture(this)`；不再调用 `Window.DragMove()`。

- [ ] **Step 3: 每次移动仅更新 `Left` 并强制固定 `Top`**

```csharp
var workArea = SystemParameters.WorkArea;
var requestedLeft = _dragStartWindowLeft + GetMouseScreenX(e) - _dragStartMouseScreenX;
Left = HorizontalWindowPlacement.ClampLeft(requestedLeft, workArea.Left, workArea.Width, Width);
Top = workArea.Top;
```

- [ ] **Step 4: 松开或丢失鼠标捕获时完成拖拽**

结束拖拽时释放捕获，将 `Top` 再次设为 `SystemParameters.WorkArea.Top`，调用 `CompleteUserDrag(Left, Top)` 并保存外观配置。结束方法必须幂等，避免主动释放捕获后重复执行。

- [ ] **Step 5: 固定启动和折叠展开的顶部基准**

启动时使用 `ResolveInitialLeft` 恢复横坐标并无条件设置 `Top = SystemParameters.WorkArea.Top`。控制器开始和结束拖拽时都更新 `_expandedTop` 为传入的主屏顶部，因此 `CollapsedTop` 始终等于主屏顶部减去卡片高度。

- [ ] **Step 6: 构建并运行全部自检**

Run: `dotnet build CodexQuotaFloat.csproj --configuration Release --no-restore`

Expected: 0 warnings, 0 errors.

Run: `dotnet run --project tests\\CodexQuotaFloat.PresentationTests\\CodexQuotaFloat.PresentationTests.csproj --configuration Release --no-restore`

Expected: 输出 `QuotaPresentation tests passed.`。

Run: `dotnet run --project tests\\CodexQuotaFloat.TokenUsageTests\\CodexQuotaFloat.TokenUsageTests.csproj --configuration Release --no-restore`

Expected: 输出 `Token usage tests passed.`。

- [ ] **Step 7: 覆盖便携发布目录并启动验证**

Run: `dotnet publish CodexQuotaFloat.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output release-floating-auto`

验证：程序启动时贴主屏顶部；左右拖动不改变纵坐标；离开 1 秒后向上折叠；悬停把手后在原横坐标展开；`appearance.json` 不再包含 `Top`。

- [ ] **Step 8: 提交实现**

```bash
git add MainWindow.xaml MainWindow.xaml.cs Services/AppearanceSettingsStore.cs Services/FloatingWindowController.cs Services/HorizontalWindowPlacement.cs tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj tests/CodexQuotaFloat.PresentationTests/Program.cs
git commit -m "fix: keep draggable window anchored to screen top"
```
